using Merjane.Application.Ports;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    /// <summary>
    /// Exposes the protected <see cref="INotificationService"/>, which must stay unchanged in this project,
    /// as the application notification port.
    /// </summary>
    /// <remarks>
    /// Each call is forwarded unchanged. The adapter lives in the API project because the protected service does,
    /// and the application layer must not reference the API.
    /// </remarks>
    public class ProductNotificationAdapter : IProductNotificationPort
    {
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Creates the adapter.
        /// </summary>
        /// <param name="notificationService">Protected notification service receiving the calls.</param>
        public ProductNotificationAdapter(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <inheritdoc/>
        public void SendDelayNotification(int leadTime, string productName)
        {
            _notificationService.SendDelayNotification(leadTime, productName);
        }

        /// <inheritdoc/>
        public void SendOutOfStockNotification(string productName)
        {
            _notificationService.SendOutOfStockNotification(productName);
        }

        /// <inheritdoc/>
        public void SendExpirationNotification(string productName, DateTime expiryDate)
        {
            _notificationService.SendExpirationNotification(productName, expiryDate);
        }
    }
}
