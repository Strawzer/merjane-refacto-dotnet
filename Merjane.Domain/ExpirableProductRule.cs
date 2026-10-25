using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    /// <summary>
    /// Availability rule for <c>EXPIRABLE</c> products, which are sold while in stock and not expired.
    /// </summary>
    public static class ExpirableProductRule
    {
        /// <summary>
        /// Sells one unit when the product is in stock and its expiry date is strictly after today's date, so a product
        /// expiring later today is still sold. Otherwise its stock is cleared with an expiration notification, even when
        /// it is merely out of stock before its expiry date.
        /// </summary>
        /// <param name="product">Ordered product of type <c>EXPIRABLE</c>.</param>
        /// <param name="now">Current date and time. Only its date is compared with the expiry date.</param>
        /// <returns>A sell decision, or the decision of <see cref="DecideWhenNotSellable"/>.</returns>
        /// <exception cref="InvalidOperationException">The product has no <see cref="Product.ExpiryDate"/>.</exception>
        public static ProductDecision Decide(Product product, DateTime now)
        {
            return product.Available > 0 && product.ExpiryDate > now.Date
                ? ProductDecision.Sell(product)
                : DecideWhenNotSellable(product, now);
        }

        /// <summary>
        /// Sells one unit when the product is in stock and its expiry date is after the current date and time;
        /// otherwise clears the stock with an expiration notification.
        /// </summary>
        /// <remarks>
        /// Compares with the time of day, unlike <see cref="Decide"/>. The sell branch is only reachable when called directly,
        /// as the legacy <c>ProductService.HandleExpiredProduct</c> does: through <see cref="Decide"/>, the expiry date is never
        /// after <paramref name="now"/> here.
        /// </remarks>
        /// <param name="product">Expirable product to evaluate.</param>
        /// <param name="now">Current date and time, compared with the expiry date including the time of day.</param>
        /// <returns>A sell decision or <see cref="ProductDecision.ClearExpiredStock"/>.</returns>
        /// <exception cref="InvalidOperationException">The product has no <see cref="Product.ExpiryDate"/>.</exception>
        public static ProductDecision DecideWhenNotSellable(Product product, DateTime now)
        {
            return product.Available > 0 && product.ExpiryDate > now
                ? ProductDecision.Sell(product)
                : ProductDecision.ClearExpiredStock(product);
        }
    }
}
