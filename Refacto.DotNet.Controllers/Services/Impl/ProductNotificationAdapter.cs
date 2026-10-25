using Merjane.Application.Ports;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    /// <summary>
    /// Exposes the protected <see cref="INotificationService"/>, which must stay unchanged in this project,
    /// as the application notification port.
    /// </summary>
    public class ProductNotificationAdapter : IProductNotificationPort
    {
        private readonly INotificationService _notificationService;

        public ProductNotificationAdapter(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public void SendDelayNotification(int leadTime, string productName)
        {
            _notificationService.SendDelayNotification(leadTime, productName);
        }

        public void SendOutOfStockNotification(string productName)
        {
            _notificationService.SendOutOfStockNotification(productName);
        }

        public void SendExpirationNotification(string productName, DateTime expiryDate)
        {
            _notificationService.SendExpirationNotification(productName, expiryDate);
        }
    }
}
