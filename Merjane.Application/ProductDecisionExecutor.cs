using Merjane.Application.Ports;
using Merjane.Domain;
using Merjane.Domain.Entities;

namespace Merjane.Application
{
    /// <summary>
    /// Applies a <see cref="ProductDecision"/>: sends its notification, updates stock, then saves the product immediately.
    /// </summary>
    /// <remarks>
    /// The single place where decisions produce side effects; both <see cref="OrderProcessingService"/> and the legacy
    /// <c>ProductService</c> of the API use it.
    /// </remarks>
    public class ProductDecisionExecutor
    {
        private readonly IProductNotificationPort _notifications;
        private readonly IOrderProcessingStore _store;

        /// <summary>
        /// Creates an executor.
        /// </summary>
        /// <param name="notifications">Delivers the decided notifications.</param>
        /// <param name="store">Saves the product after each decision that requires it.</param>
        public ProductDecisionExecutor(IProductNotificationPort notifications, IOrderProcessingStore store)
        {
            _notifications = notifications;
            _store = store;
        }

        /// <summary>
        /// Applies a decision to a product: sends the notification, then updates the stock, then saves.
        /// </summary>
        /// <remarks>
        /// <see cref="ProductDecision.None"/> does nothing. When the notification fails, the product is neither changed nor
        /// saved; when the save fails, the notification has already been sent.
        /// </remarks>
        /// <param name="product">Tracked product the decision applies to.</param>
        /// <param name="decision">Decision returned by the availability rules.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The decision carries a notification type the executor does not know; this cannot happen with the domain notification types.
        /// </exception>
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

        /// <summary>
        /// Forwards a domain notification to the matching notification port method, with unchanged arguments.
        /// </summary>
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
