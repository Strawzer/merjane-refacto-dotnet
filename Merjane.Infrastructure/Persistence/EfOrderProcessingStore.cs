using Merjane.Application.Ports;
using Merjane.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Merjane.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core implementation of <see cref="IOrderProcessingStore"/> backed by <see cref="AppDbContext"/>.
    /// </summary>
    public class EfOrderProcessingStore : IOrderProcessingStore
    {
        private readonly AppDbContext _ctx;

        /// <summary>
        /// Creates a store over the given context, whose change tracker holds the loaded entities until saved.
        /// </summary>
        /// <param name="ctx">Context shared with the rest of the request scope.</param>
        public EfOrderProcessingStore(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        /// <inheritdoc/>
        public Order? FindOrderWithProducts(long orderId)
        {
            return _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefault(o => o.Id == orderId);
        }

        /// <inheritdoc/>
        public void SaveChanges()
        {
            _ = _ctx.SaveChanges();
        }
    }
}
