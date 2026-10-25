namespace Merjane.Application
{
    /// <summary>
    /// Thrown when the order to process does not exist.
    /// </summary>
    /// <remarks>
    /// Deliberately not mapped to an HTTP status: as with the <see cref="NullReferenceException"/> of the original code,
    /// an unknown order results in HTTP 500.
    /// </remarks>
    public class OrderNotFoundException : Exception
    {
        /// <summary>
        /// Creates the exception for the given order.
        /// </summary>
        /// <param name="orderId">Identifier of the missing order.</param>
        public OrderNotFoundException(long orderId)
            : base($"Order {orderId} was not found.")
        {
            OrderId = orderId;
        }

        /// <summary>
        /// Identifier of the order that was not found.
        /// </summary>
        public long OrderId { get; }
    }
}
