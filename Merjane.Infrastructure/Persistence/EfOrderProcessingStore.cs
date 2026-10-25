using Merjane.Application.Ports;
using Merjane.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Merjane.Infrastructure.Persistence
{
    public class EfOrderProcessingStore : IOrderProcessingStore
    {
        private readonly AppDbContext _ctx;

        public EfOrderProcessingStore(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public Order? FindOrderWithProducts(long orderId)
        {
            return _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefault(o => o.Id == orderId);
        }

        public void SaveChanges()
        {
            _ = _ctx.SaveChanges();
        }
    }
}
