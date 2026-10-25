using Merjane.Application.Ports;
using Merjane.Domain;
using Merjane.Domain.Entities;

namespace Merjane.Application
{
    public class OrderProcessingService
    {
        private readonly IOrderProcessingStore _store;
        private readonly ProductDecisionExecutor _executor;
        private readonly IClock _clock;

        public OrderProcessingService(IOrderProcessingStore store, ProductDecisionExecutor executor, IClock clock)
        {
            _store = store;
            _executor = executor;
            _clock = clock;
        }

        /// <summary>
        /// Applies the availability rule of each ordered product, saving each product as soon as it is handled.
        /// </summary>
        /// <exception cref="OrderNotFoundException">The order does not exist.</exception>
        public long ProcessOrder(long orderId)
        {
            Order order = _store.FindOrderWithProducts(orderId)
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
