using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Domain
{
    /// <summary>
    /// Outcome of an availability rule for one ordered product.
    /// The notification, when present, is sent before the stock update is applied and saved,
    /// as in the legacy out-of-stock and expiration paths.
    /// </summary>
    /// <param name="NewAvailable">Stock quantity to store, or <c>null</c> to leave it unchanged.</param>
    /// <param name="Notification">Customer notification to send, if any.</param>
    public sealed record ProductDecision(int? NewAvailable, ProductNotification? Notification)
    {
        public static ProductDecision None { get; } = new(null, null);

        public bool RequiresSave => NewAvailable is not null || Notification is not null;

        public static ProductDecision Sell(Product product)
        {
            return new(product.Available - 1, null);
        }

        public static ProductDecision NotifyDelay(Product product)
        {
            return new(null, new DelayNotification(product.Name!, product.LeadTime));
        }

        public static ProductDecision NotifyOutOfStock(Product product, bool clearStock)
        {
            return new(clearStock ? 0 : null, new OutOfStockNotification(product.Name!));
        }

        public static ProductDecision ClearExpiredStock(Product product)
        {
            return new(0, new ExpirationNotification(product.Name!, product.ExpiryDate!.Value));
        }
    }
}
