namespace Merjane.Application
{
    public class OrderNotFoundException : Exception
    {
        public OrderNotFoundException(long orderId)
            : base($"Order {orderId} was not found.")
        {
            OrderId = orderId;
        }

        public long OrderId { get; }
    }
}
