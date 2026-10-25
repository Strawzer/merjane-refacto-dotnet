using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    public static class NormalProductRule
    {
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
