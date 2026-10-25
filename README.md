### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise
`
* Pour lancer les tests: `dotnet test`

---

## Refactoring notes

### Architecture

The production code is split into four `net10.0` projects. The API project keeps its original name, assembly and namespaces (`Refacto.DotNet.Controllers`) for compatibility.

| Project | Main files | Responsibility |
|---|---|---|
| `Merjane.Domain` | `Entities/Product.cs`, `Entities/Order.cs`, `NormalProductRule.cs`, `SeasonalProductRule.cs`, `ExpirableProductRule.cs`, `ProductAvailabilityRules.cs`, `ProductDecision.cs`, `ProductNotification.cs` | Entities and pure business decisions per product type. The current time is a parameter; no EF Core, ASP.NET Core, clock or notification dependency. |
| `Merjane.Application` | `OrderProcessingService.cs`, `ProductDecisionExecutor.cs`, `OrderNotFoundException.cs`, `Ports/IOrderProcessingStore.cs`, `Ports/IProductNotificationPort.cs`, `Ports/IClock.cs` | Order processing use case: loads the order, reads the clock once, applies the rule of each product, then sends the notification, updates stock and saves, product by product. |
| `Merjane.Infrastructure` | `Persistence/AppDbContext.cs`, `Persistence/EfOrderProcessingStore.cs`, `SystemClock.cs`, `InfrastructureServiceCollectionExtensions.cs` | EF Core InMemory persistence and system clock, registered by `AddInfrastructure()`. |
| `Refacto.DotNet.Controllers` (API) | `Controllers/OrdersController.cs`, `Dtos/`, `Program.cs`, `Services/Impl/ProductNotificationAdapter.cs`, protected `Services/INotificationService.cs` and `Services/Impl/NotificationService.cs`, legacy `Services/Impl/ProductService.cs` | `POST /orders/{orderId}/processOrder`, Swagger and the dependency injection composition root. The controller only calls the use case and returns `ProcessOrderResponse`. |

Project references:

- `Merjane.Domain`: none.
- `Merjane.Application` -> `Merjane.Domain`.
- `Merjane.Infrastructure` -> `Merjane.Application`, `Merjane.Domain`.
- API -> `Merjane.Application`, `Merjane.Infrastructure`, and `Merjane.Domain` for the legacy `ProductService` only.
- `Refacto.Dotnet.Controllers.Tests` -> all four projects. `Architecture/LayerDependencyTests` fails if the compiled Domain, Application or Infrastructure assembly references an outer layer, if Domain or Application references EF Core, or if any of them references ASP.NET Core.

**Application ports.** The use case needs three things from outside: loading an order with its products and saving tracked changes (`IOrderProcessingStore`), sending notifications (`IProductNotificationPort`) and the current time (`IClock`). The interfaces are declared in Application and implemented by outer projects, so Application depends on Domain only. `IOrderProcessingStore` is specific to order processing, not a generic repository or unit of work, and stays synchronous like the original code.

**EF Core persistence.** `EfOrderProcessingStore` runs the original tracked query (`Include(o => o.Items)` then `SingleOrDefault`) and its `SaveChanges()` calls `AppDbContext.SaveChanges()`. Products therefore keep the order returned by EF Core, and each product is still saved as soon as it is handled, without a transaction. The EF model, the `InMemoryDb` store and the existing registration lifetimes are the same as before the split.

**Protected notification adapter.** `INotificationService` and `NotificationService` must not be modified or moved, so they stay in the API project. `ProductNotificationAdapter` implements `IProductNotificationPort` by forwarding each call unchanged to `INotificationService`; both are registered in `Program.cs`. The port mirrors the three protected methods, so notification types, arguments and order are unchanged, and Application never references `INotificationService`.

**Compromises.**

- The legacy `ProductService` keeps its public constructor `(INotificationService, AppDbContext)` and methods for its existing unit tests. It builds its own `ProductNotificationAdapter`, `EfOrderProcessingStore` and `SystemClock`, then calls the domain rules and the shared `ProductDecisionExecutor`, so no business rule is duplicated. It still depends on concrete Infrastructure types and on the system clock rather than on the ports, bypasses dependency injection (it is not registered) and is the only reason the API references `Merjane.Domain` directly.
- Domain entities keep their persistence shape: mutable properties and mapping attributes from `System.ComponentModel.DataAnnotations` (`[Table]`, `[Column]`, `[Key]`). These come from the base class library, not EF Core.
- `IOrderProcessingStore.SaveChanges()` relies on change tracking: an implementation must return tracked entities so that stock updates made on them are persisted.

Build, test and run (Swagger UI at `http://localhost:5122/swagger` in the Development environment):

```powershell
dotnet build Refacto.DotNet.Controllers.sln
dotnet test Refacto.DotNet.Controllers.sln
dotnet run --project Refacto.DotNet.Controllers --launch-profile http
```

### Running the tests

The projects target `net10.0` (EF Core 10, ASP.NET Core 10) and require the .NET 10 SDK:

```powershell
dotnet test
```

Date-dependent tests use a fixed clock and do not depend on the system date. The assessment delivery on `net7.0` is tagged `merjane-assessment-baseline-73tests`.

### Preserved legacy behaviors

These behaviors are covered by tests and intentionally kept, even where they look questionable:

- An unknown order returns HTTP 500 (now through an explicit `OrderNotFoundException` instead of a `NullReferenceException`).
- Each ordered product decreases stock by exactly one unit; orders carry no quantity.
- NORMAL out of stock with a lead time of 0 sends no notification.
- SEASONAL products are sold only strictly between the season start and end dates compared to today's date, so they are not sold on the first or last day of the season.
- SEASONAL products that cannot be sold compare season dates with the current time of day: replenishment ending after the season end clears the stock and sends an out-of-stock notification; a season starting later sends an out-of-stock notification and keeps the stock; otherwise (including missing season dates) a delay notification is sent.
- EXPIRABLE products are sold while the expiry date is after today's date; a product expiring later today is still sold.
- EXPIRABLE products out of stock but not expired send an expiration notification and keep a stock of 0.
- EXPIRABLE products without an expiry date fail with `InvalidOperationException`, stopping the order.
- Unknown product types are ignored.
- Each product is saved as soon as it is handled, without a transaction: a failure on one product keeps earlier products saved and leaves later products untouched.

### Intentional differences from the original implementation

- Notifications are always sent before the stock update and save. The original delay path saved before notifying; that save never had pending changes during order processing, so stored data and notifications are unchanged, but a direct `ProductService.NotifyDelay` call on an entity with unsaved changes would now notify before persisting them.
- The clock is read once per order instead of several times per product, which only differs from the original for a request running across midnight.
- The explicit `EntityState.Modified` marking of the NORMAL branch was dropped; tracked change detection persists the same values (on a relational database only the changed column would be updated).
- `Console.WriteLine(order)` was removed and `ProductService` is no longer registered in dependency injection.

### Known limitations and technical debt

- Product types are strings; an enum or value object would require an entity change.
- `ProductService` can be removed together with its legacy unit tests.
- Product iteration follows the order in which EF Core returns order items, as in the original code.
