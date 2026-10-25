using Merjane.Domain.Entities;

namespace Merjane.Application.Ports
{
    /// <summary>
    /// Persistence used by order processing. The loaded order and its products stay tracked:
    /// <see cref="SaveChanges"/> persists every change made to them since the previous save.
    /// </summary>
    /// <remarks>
    /// Deliberately specific to order processing rather than a generic repository, and synchronous like the original code.
    /// </remarks>
    public interface IOrderProcessingStore
    {
        /// <summary>
        /// Loads an order together with its products, tracked for later saves.
        /// </summary>
        /// <param name="orderId">Identifier of the order.</param>
        /// <returns>
        /// The order with <see cref="Order.Items"/> loaded, in the order provided by the storage, or <see langword="null"/>
        /// when no order has this identifier.
        /// </returns>
        Order? FindOrderWithProducts(long orderId);

        /// <summary>
        /// Persists every change made to the loaded entities since the previous save.
        /// </summary>
        /// <remarks>
        /// Called once for each product whose decision requires a save, so products handled earlier stay saved when a later one fails.
        /// </remarks>
        void SaveChanges();
    }
}
