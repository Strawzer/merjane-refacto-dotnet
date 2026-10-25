using Merjane.Domain.Entities;

namespace Merjane.Application.Ports
{
    /// <summary>
    /// Persistence used by order processing. The loaded order and its products stay tracked:
    /// <see cref="SaveChanges"/> persists every change made to them since the previous save.
    /// </summary>
    public interface IOrderProcessingStore
    {
        Order? FindOrderWithProducts(long orderId);

        void SaveChanges();
    }
}
