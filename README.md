### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise
`
* Pour lancer les tests: `dotnet test`

---

## Refactoring notes

### Architecture

| Layer | Files | Responsibility |
|---|---|---|
| API | `Controllers/MyController.cs` (`OrdersController`) | Maps `POST /orders/{orderId}/processOrder` to the use case and returns `ProcessOrderResponse`. No business rules or data access. |
| Application | `Services/Impl/OrderProcessingService.cs` | Loads the order with its products, reads the clock once, applies the rule of each product. |
| Application | `Services/Impl/ProductDecisionExecutor.cs` | Executes a decision: sends the notification, updates stock, saves the product. |
| Application | `Services/IClock.cs`, `Services/Impl/SystemClock.cs` | Current time, injected so that date rules can be tested deterministically. |
| Domain | `Domain/NormalProductRule.cs`, `SeasonalProductRule.cs`, `ExpirableProductRule.cs`, `ProductAvailabilityRules.cs` | Pure business decisions per product type; no EF Core, ASP.NET Core, clock or notification dependency. |
| Domain | `Domain/ProductDecision.cs`, `Domain/ProductNotification.cs` | Explicit rule outcome: new stock quantity and notification payload. |
| Persistence | `Database/Context/AppDbContext.cs`, `Entities/*` | EF Core model (unchanged). |
| Legacy | `Services/Impl/ProductService.cs` | Compatibility adapter keeping the original public methods for their existing unit tests. It delegates to the domain rules, is not registered in dependency injection and is not used by production code. |

The application services use `AppDbContext` directly as their unit of work; no repository layer was added for a single query. Domain rules only depend on the `Product` entity (plain properties with data annotations) and receive the current time as a parameter.

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
