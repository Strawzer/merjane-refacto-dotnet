using Merjane.Application;
using Merjane.Domain;
using Merjane.Domain.Entities;
using Merjane.Infrastructure;
using Merjane.Infrastructure.Persistence;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    /// <summary>
    /// Legacy entry points kept for compatibility. Order processing goes through <see cref="OrderProcessingService"/>.
    /// </summary>
    public class ProductService
    {
        private readonly ProductDecisionExecutor _executor;
        private readonly SystemClock _clock = new();

        public ProductService(INotificationService ns, AppDbContext ctx)
        {
            _executor = new ProductDecisionExecutor(new ProductNotificationAdapter(ns), new EfOrderProcessingStore(ctx));
        }

        public void NotifyDelay(int leadTime, Product p)
        {
            p.LeadTime = leadTime;
            _executor.Execute(p, ProductDecision.NotifyDelay(p));
        }

        public void HandleSeasonalProduct(Product p)
        {
            _executor.Execute(p, SeasonalProductRule.DecideWhenNotSellable(p, _clock.Now));
        }

        public void HandleExpiredProduct(Product p)
        {
            _executor.Execute(p, ExpirableProductRule.DecideWhenNotSellable(p, _clock.Now));
        }
    }
}
