namespace Merjane.Domain
{
    /// <summary>
    /// Customer notification decided by an availability rule and delivered by the application layer.
    /// </summary>
    /// <param name="ProductName">Name of the product concerned, copied from <see cref="Entities.Product.Name"/> without validation.</param>
    public abstract record ProductNotification(string ProductName);

    /// <summary>
    /// Tells the customer that a product will be available again after its lead time.
    /// </summary>
    /// <param name="ProductName">Name of the product concerned.</param>
    /// <param name="LeadTime">Replenishment lead time in days, copied from <see cref="Entities.Product.LeadTime"/>.</param>
    public sealed record DelayNotification(string ProductName, int LeadTime) : ProductNotification(ProductName);

    /// <summary>
    /// Tells the customer that a seasonal product cannot be supplied: its season has not started yet,
    /// or replenishment would end after the season.
    /// </summary>
    /// <param name="ProductName">Name of the product concerned.</param>
    public sealed record OutOfStockNotification(string ProductName) : ProductNotification(ProductName);

    /// <summary>
    /// Tells the customer that an expirable product is no longer sold.
    /// </summary>
    /// <remarks>
    /// As in the original code, it is also sent for a product that is merely out of stock before its expiry date.
    /// </remarks>
    /// <param name="ProductName">Name of the product concerned.</param>
    /// <param name="ExpiryDate">Expiry date of the product, copied from <see cref="Entities.Product.ExpiryDate"/>.</param>
    public sealed record ExpirationNotification(string ProductName, DateTime ExpiryDate) : ProductNotification(ProductName);
}
