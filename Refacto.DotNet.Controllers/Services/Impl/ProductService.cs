using Merjane.Application;
using Merjane.Domain;
using Merjane.Domain.Entities;
using Merjane.Infrastructure;
using Merjane.Infrastructure.Persistence;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    /// <summary>
    /// Legacy product entry points. Order processing goes through <see cref="OrderProcessingService"/>.
    /// </summary>
    /// <remarks>
    /// Not registered in dependency injection and not used by the API; kept for the existing legacy test scenarios.
    /// Method names and parameter shapes are unchanged, but the parameter types moved to other namespaces and assemblies,
    /// so source and binary compatibility are not guaranteed for external callers. It composes its dependencies itself and
    /// delegates to the domain rules and <see cref="ProductDecisionExecutor"/>, so it shares the order processing behavior. This is why the API project still references the domain project.
    /// </remarks>
    public class ProductService
    {
        private readonly ProductDecisionExecutor _executor;
        private readonly SystemClock _clock = new();

        /// <summary>
        /// Creates the service over the given notification service and context.
        /// </summary>
        /// <param name="ns">Notification service receiving the notifications.</param>
        /// <param name="ctx">Context that tracks the products passed to the methods and saves them.</param>
        public ProductService(INotificationService ns, AppDbContext ctx)
        {
            _executor = new ProductDecisionExecutor(new ProductNotificationAdapter(ns), new EfOrderProcessingStore(ctx));
        }

        /// <summary>
        /// Sets the product's lead time, sends a delay notification with it and saves.
        /// </summary>
        /// <remarks>
        /// The notification is now sent before the save; the original code saved first. Successful calls have the same
        /// outcome, but the difference is observable when a call fails: if the notification fails, the new lead time is
        /// not saved; if the save fails, the notification has already been sent.
        /// </remarks>
        /// <param name="leadTime">New lead time in days.</param>
        /// <param name="p">Tracked product.</param>
        public void NotifyDelay(int leadTime, Product p)
        {
            p.LeadTime = leadTime;
            _executor.Execute(p, ProductDecision.NotifyDelay(p));
        }

        /// <summary>
        /// Applies <see cref="SeasonalProductRule.DecideWhenNotSellable"/> at the current local time, then notifies and saves.
        /// </summary>
        /// <param name="p">Tracked seasonal product.</param>
        public void HandleSeasonalProduct(Product p)
        {
            _executor.Execute(p, SeasonalProductRule.DecideWhenNotSellable(p, _clock.Now));
        }

        /// <summary>
        /// Applies <see cref="ExpirableProductRule.DecideWhenNotSellable"/> at the current local time: sells one unit when
        /// the product is in stock and not yet expired, otherwise clears the stock and sends an expiration notification.
        /// </summary>
        /// <param name="p">Tracked expirable product.</param>
        /// <exception cref="InvalidOperationException">The product has no <see cref="Product.ExpiryDate"/>.</exception>
        public void HandleExpiredProduct(Product p)
        {
            _executor.Execute(p, ExpirableProductRule.DecideWhenNotSellable(p, _clock.Now));
        }
    }
}
