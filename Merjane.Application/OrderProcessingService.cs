using Merjane.Application.Ports;
using Merjane.Domain;
using Merjane.Domain.Entities;

namespace Merjane.Application
{
    /// <summary>
    /// Order processing use case: applies the availability rule of each ordered product and persists the outcome
    /// product by product.
    /// </summary>
    /// <remarks>
    /// Depends only on the domain and on application ports, so it has no EF Core, ASP.NET Core or host dependency.
    /// </remarks>
    public class OrderProcessingService
    {
        private readonly IOrderProcessingStore _store;
        private readonly ProductDecisionExecutor _executor;
        private readonly IClock _clock;

        /// <summary>
        /// Creates the use case.
        /// </summary>
        /// <param name="store">Loads the order and saves each handled product.</param>
        /// <param name="executor">Applies each product decision.</param>
        /// <param name="clock">Provides the time the order is evaluated at.</param>
        public OrderProcessingService(IOrderProcessingStore store, ProductDecisionExecutor executor, IClock clock)
        {
            _store = store;
            _executor = executor;
            _clock = clock;
        }

        /// <summary>
        /// Applies the availability rule of each ordered product, saving each product as soon as it is handled.
        /// </summary>
        /// <remarks>
        /// The clock is read once, so every product of the order is evaluated against the same time. Products are handled
        /// in the order returned by the store. Each decision is fully executed (notification, stock update, save) before the
        /// next product, without a transaction: when a rule, a notification or a save fails, the exception propagates,
        /// products handled earlier stay saved and later products are left untouched.
        /// </remarks>
        /// <param name="orderId">Identifier of the order to process.</param>
        /// <returns>The identifier of the processed order.</returns>
        /// <exception cref="OrderNotFoundException">The order does not exist.</exception>
        /// <exception cref="InvalidOperationException">An <c>EXPIRABLE</c> product of the order has no expiry date.</exception>
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
