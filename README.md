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
| API | `Controllers/MyController.cs` (`OrdersController`) | Maps `POST /orders/{orderId}/processOrder` to the use case and returns `ProcessOrderResponse`. |
| Application | `Services/Impl/OrderProcessingService.cs` | Loads the order with its products, captures the current time once, applies the rule of each product. |
| Application | `Services/Impl/ProductDecisionExecutor.cs` | Executes a decision: sends the notification, updates stock, saves the product. |
| Domain | `Domain/NormalProductRule.cs`, `SeasonalProductRule.cs`, `ExpirableProductRule.cs`, `ProductAvailabilityRules.cs` | Pure business decisions per product type; no EF Core, ASP.NET Core or notification dependency. |
| Domain | `Domain/ProductDecision.cs`, `Domain/ProductNotification.cs` | Explicit rule outcome: new stock quantity and notification payload. |
| Infrastructure | `Services/IClock.cs`, `Services/Impl/SystemClock.cs`, `Database/Context/AppDbContext.cs` | Current time and persistence. |
| Legacy | `Services/Impl/ProductService.cs` | Compatibility adapter with the original public API, delegating to the domain rules. Not used by production code. |

### Running the tests

The projects target `net7.0`. If the .NET 7 runtime is not installed, run the tests on a newer installed runtime through roll-forward (PowerShell):

```powershell
$env:DOTNET_ROLL_FORWARD = "Major"
dotnet test
```

The refactoring was validated this way (SDK 10.0.303, .NET 8+ runtime), not natively on .NET 7.

### Preserved legacy behaviors

These behaviors are covered by characterization tests and intentionally kept, even where they look questionable:

- An unknown order returns HTTP 500 (now through an explicit `OrderNotFoundException` instead of a `NullReferenceException`).
- Each ordered product decreases stock by exactly one unit; orders carry no quantity.
- NORMAL out of stock with a lead time of 0 sends no notification.
- SEASONAL products are sold only strictly between the season start and end dates (compared to today's date), so they are not sold on the first or last day of the season.
- SEASONAL products that cannot be sold: replenishment ending after the season end (compared with the time of day) clears the stock and sends an out-of-stock notification; a season not started yet sends an out-of-stock notification and keeps the stock; otherwise a delay notification is sent.
- EXPIRABLE products are sold while the expiry date is after today's date; a product expiring later today is still sold.
- EXPIRABLE products out of stock but not expired send an expiration notification and keep a stock of 0.
- EXPIRABLE products without an expiry date fail with `InvalidOperationException`.
- Unknown product types are ignored.
- Each product is saved as soon as it is handled: a failure on one product keeps earlier products updated.

### Known limitations and technical debt

- Product types are strings; an enum or value object would require an entity change.
- `ProductService` is kept only for compatibility with its existing tests and can be removed together with them.
- The explicit `EntityState.Modified` marking of the original NORMAL branch was dropped; tracked change detection persists the same values.
- No transaction around an order, by design, to keep the original partial-update behavior.
- The original `ProcessOrderShouldReturn` integration test still has a nullable warning; existing tests were not modified.
