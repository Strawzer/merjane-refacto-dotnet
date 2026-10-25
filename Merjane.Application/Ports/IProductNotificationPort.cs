namespace Merjane.Application.Ports
{
    /// <summary>
    /// Delivers the customer notifications decided by the availability rules.
    /// </summary>
    /// <remarks>
    /// Declared in the application layer so that it does not depend on the host's protected notification service.
    /// The methods mirror that service, so notification types, arguments and order are preserved by an adapter that
    /// forwards each call unchanged.
    /// </remarks>
    public interface IProductNotificationPort
    {
        /// <summary>
        /// Notifies that a product will be available again after its lead time.
        /// </summary>
        /// <param name="leadTime">Replenishment lead time in days.</param>
        /// <param name="productName">Name of the product.</param>
        void SendDelayNotification(int leadTime, string productName);

        /// <summary>
        /// Notifies that a seasonal product cannot be supplied.
        /// </summary>
        /// <param name="productName">Name of the product.</param>
        void SendOutOfStockNotification(string productName);

        /// <summary>
        /// Notifies that an expirable product is no longer sold, including when it is merely out of stock.
        /// </summary>
        /// <param name="productName">Name of the product.</param>
        /// <param name="expiryDate">Expiry date of the product.</param>
        void SendExpirationNotification(string productName, DateTime expiryDate);
    }
}
