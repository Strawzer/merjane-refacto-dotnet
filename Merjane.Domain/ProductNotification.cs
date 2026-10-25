namespace Merjane.Domain
{
    public abstract record ProductNotification(string ProductName);

    public sealed record DelayNotification(string ProductName, int LeadTime) : ProductNotification(ProductName);

    public sealed record OutOfStockNotification(string ProductName) : ProductNotification(ProductName);

    public sealed record ExpirationNotification(string ProductName, DateTime ExpiryDate) : ProductNotification(ProductName);
}
