using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    /// <summary>
    /// Entry point of the availability rules: selects the rule matching the product type.
    /// </summary>
    public static class ProductAvailabilityRules
    {
        /// <summary>
        /// Decides how an ordered product is handled. Unknown product types are left untouched.
        /// </summary>
        /// <param name="product">Ordered product.</param>
        /// <param name="now">Current date and time, captured once for the whole order.</param>
        /// <returns>
        /// The decision of <see cref="NormalProductRule"/>, <see cref="SeasonalProductRule"/> or <see cref="ExpirableProductRule"/>
        /// for the types <c>NORMAL</c>, <c>SEASONAL</c> and <c>EXPIRABLE</c> (exact, case-sensitive match);
        /// <see cref="ProductDecision.None"/> for any other type.
        /// </returns>
        /// <exception cref="InvalidOperationException">An <c>EXPIRABLE</c> product has no <see cref="Product.ExpiryDate"/>.</exception>
        public static ProductDecision Decide(Product product, DateTime now)
        {
            return product.Type switch
            {
                "NORMAL" => NormalProductRule.Decide(product),
                "SEASONAL" => SeasonalProductRule.Decide(product, now),
                "EXPIRABLE" => ExpirableProductRule.Decide(product, now),
                _ => ProductDecision.None,
            };
        }
    }
}
