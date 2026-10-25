using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    /// <summary>
    /// Outcome of an availability rule for one ordered product: the stock quantity to store and the notification to send.
    /// </summary>
    /// <remarks>
    /// Rules only describe the outcome; the application layer applies it. The notification, when present, is sent before
    /// the stock update is applied and saved, as in the legacy out-of-stock and expiration paths.
    /// </remarks>
    /// <param name="NewAvailable">Stock quantity to store, or <see langword="null"/> to leave it unchanged.</param>
    /// <param name="Notification">Customer notification to send, or <see langword="null"/> for none.</param>
    public sealed record ProductDecision(int? NewAvailable, ProductNotification? Notification)
    {
        /// <summary>
        /// Leaves the product untouched: no stock change, no notification and no save.
        /// </summary>
        public static ProductDecision None { get; } = new(null, null);

        /// <summary>
        /// Whether the product must be saved once the decision is applied.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when the decision changes the stock or sends a notification. A notification without
        /// stock change still triggers a save, as in the original code, even though the product itself did not change.
        /// </value>
        public bool RequiresSave => NewAvailable is not null || Notification is not null;

        /// <summary>
        /// Sells one unit: stores the current stock minus one, without notification.
        /// </summary>
        /// <param name="product">Product being sold. The rules call this only when at least one unit is in stock.</param>
        /// <returns>A decision that decrements the stock by one.</returns>
        public static ProductDecision Sell(Product product)
        {
            return new(product.Available - 1, null);
        }

        /// <summary>
        /// Keeps the stock unchanged and sends a <see cref="DelayNotification"/> with the product's current lead time.
        /// </summary>
        /// <param name="product">Product that will be available again after its lead time.</param>
        /// <returns>A decision that only notifies a delay.</returns>
        public static ProductDecision NotifyDelay(Product product)
        {
            return new(null, new DelayNotification(product.Name!, product.LeadTime));
        }

        /// <summary>
        /// Sends an <see cref="OutOfStockNotification"/>, optionally clearing the stock.
        /// </summary>
        /// <param name="product">Seasonal product that cannot be supplied.</param>
        /// <param name="clearStock"><see langword="true"/> to set the stock to zero; <see langword="false"/> to keep it unchanged.</param>
        /// <returns>A decision that notifies an out-of-stock situation.</returns>
        public static ProductDecision NotifyOutOfStock(Product product, bool clearStock)
        {
            return new(clearStock ? 0 : null, new OutOfStockNotification(product.Name!));
        }

        /// <summary>
        /// Sets the stock to zero and sends an <see cref="ExpirationNotification"/> with the product's expiry date.
        /// </summary>
        /// <param name="product">Expirable product that can no longer be sold.</param>
        /// <returns>A decision that clears the stock and notifies the expiration.</returns>
        /// <exception cref="InvalidOperationException">The product has no <see cref="Product.ExpiryDate"/>.</exception>
        public static ProductDecision ClearExpiredStock(Product product)
        {
            return new(0, new ExpirationNotification(product.Name!, product.ExpiryDate!.Value));
        }
    }
}
