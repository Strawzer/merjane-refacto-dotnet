using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Domain
{
    public static class ExpirableProductRule
    {
        /// <summary>
        /// An expirable product is sold while in stock and its expiry date is after today's date.
        /// Otherwise its stock is cleared with an expiration notification, even when it is merely out of stock.
        /// </summary>
        public static ProductDecision Decide(Product product, DateTime now)
        {
            return product.Available > 0 && product.ExpiryDate > now.Date
                ? ProductDecision.Sell(product)
                : DecideWhenNotSellable(product, now);
        }

        /// <summary>
        /// Compares the expiry date with the current time of day, unlike <see cref="Decide"/>.
        /// The sell branch is only reachable when called directly: through <see cref="Decide"/>, the expiry date is never after <paramref name="now"/>.
        /// Throws <see cref="InvalidOperationException"/> when the product has no expiry date.
        /// </summary>
        public static ProductDecision DecideWhenNotSellable(Product product, DateTime now)
        {
            return product.Available > 0 && product.ExpiryDate > now
                ? ProductDecision.Sell(product)
                : ProductDecision.ClearExpiredStock(product);
        }
    }
}
