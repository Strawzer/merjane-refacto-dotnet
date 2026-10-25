using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    public static class ProductAvailabilityRules
    {
        /// <summary>
        /// Decides how an ordered product is handled. Unknown product types are left untouched.
        /// </summary>
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
