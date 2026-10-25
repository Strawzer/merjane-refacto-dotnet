namespace Merjane.Application.Ports
{
    public interface IProductNotificationPort
    {
        void SendDelayNotification(int leadTime, string productName);

        void SendOutOfStockNotification(string productName);

        void SendExpirationNotification(string productName, DateTime expiryDate);
    }
}
