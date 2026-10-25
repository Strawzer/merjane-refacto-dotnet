using Merjane.Domain;
using Merjane.Domain.Entities;
using Refacto.DotNet.Controllers.Database.Context;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    /// <summary>
    /// Applies a <see cref="ProductDecision"/>: sends its notification, updates stock, then saves the product immediately.
    /// </summary>
    public class ProductDecisionExecutor
    {
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _ctx;

        public ProductDecisionExecutor(INotificationService notificationService, AppDbContext ctx)
        {
            _notificationService = notificationService;
            _ctx = ctx;
        }

        public void Execute(Product product, ProductDecision decision)
        {
            if (decision.Notification is not null)
            {
                Send(decision.Notification);
            }

            if (decision.NewAvailable is int available)
            {
                product.Available = available;
            }

            if (decision.RequiresSave)
            {
                _ = _ctx.SaveChanges();
            }
        }

        private void Send(ProductNotification notification)
        {
            switch (notification)
            {
                case DelayNotification delay:
                    _notificationService.SendDelayNotification(delay.LeadTime, delay.ProductName);
                    break;
                case OutOfStockNotification outOfStock:
                    _notificationService.SendOutOfStockNotification(outOfStock.ProductName);
                    break;
                case ExpirationNotification expiration:
                    _notificationService.SendExpirationNotification(expiration.ProductName, expiration.ExpiryDate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(notification), notification.GetType().Name, "Unsupported notification.");
            }
        }
    }
}
