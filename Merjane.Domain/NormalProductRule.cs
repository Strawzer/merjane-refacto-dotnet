using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    /// <summary>
    /// Availability rule for <c>NORMAL</c> products: sold while in stock, otherwise replenished after their lead time.
    /// </summary>
    public static class NormalProductRule
    {
        /// <summary>
        /// Decides how an ordered normal product is handled.
        /// </summary>
        /// <param name="product">Ordered product of type <c>NORMAL</c>.</param>
        /// <returns>
        /// <see cref="ProductDecision.Sell"/> when at least one unit is in stock;
        /// <see cref="ProductDecision.NotifyDelay"/> when out of stock with a positive lead time, keeping stock and lead time unchanged;
        /// otherwise <see cref="ProductDecision.None"/>, so an out-of-stock product without lead time sends no notification and is not saved.
        /// </returns>
        public static ProductDecision Decide(Product product)
        {
            if (product.Available > 0)
            {
                return ProductDecision.Sell(product);
            }

            return product.LeadTime > 0 ? ProductDecision.NotifyDelay(product) : ProductDecision.None;
        }
    }
}
