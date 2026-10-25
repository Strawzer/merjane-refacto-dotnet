using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Domain;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class OrderProcessingService
    {
        private readonly AppDbContext _ctx;
        private readonly ProductDecisionExecutor _executor;
        private readonly IClock _clock;

        public OrderProcessingService(AppDbContext ctx, ProductDecisionExecutor executor, IClock clock)
        {
            _ctx = ctx;
            _executor = executor;
            _clock = clock;
        }

        /// <summary>
        /// Applies the availability rule of each ordered product, saving each product as soon as it is handled.
        /// </summary>
        /// <exception cref="OrderNotFoundException">The order does not exist.</exception>
        public long ProcessOrder(long orderId)
        {
            Order order = _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefault(o => o.Id == orderId)
                ?? throw new OrderNotFoundException(orderId);

            DateTime now = _clock.Now;
            foreach (Product product in order.Items!)
            {
                _executor.Execute(product, ProductAvailabilityRules.Decide(product, now));
            }

            return order.Id;
        }
    }
}
