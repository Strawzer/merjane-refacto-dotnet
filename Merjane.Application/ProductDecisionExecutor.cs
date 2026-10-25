using Merjane.Application.Ports;
using Merjane.Domain;
using Merjane.Domain.Entities;

namespace Merjane.Application
{
    /// <summary>
    /// Applies a <see cref="ProductDecision"/>: sends its notification, updates stock, then saves the product immediately.
    /// </summary>
    public class ProductDecisionExecutor
    {
        private readonly IProductNotificationPort _notifications;
        private readonly IOrderProcessingStore _store;

        public ProductDecisionExecutor(IProductNotificationPort notifications, IOrderProcessingStore store)
        {
            _notifications = notifications;
            _store = store;
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
                _store.SaveChanges();
            }
        }

        private void Send(ProductNotification notification)
        {
            switch (notification)
            {
                case DelayNotification delay:
                    _notifications.SendDelayNotification(delay.LeadTime, delay.ProductName);
                    break;
                case OutOfStockNotification outOfStock:
                    _notifications.SendOutOfStockNotification(outOfStock.ProductName);
                    break;
                case ExpirationNotification expiration:
                    _notifications.SendExpirationNotification(expiration.ProductName, expiration.ExpiryDate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(notification), notification.GetType().Name, "Unsupported notification.");
            }
        }
    }
}
