using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Domain
{
    public static class SeasonalProductRule
    {
        /// <summary>
        /// A seasonal product is sold only strictly between its season start and end dates, compared to today's date.
        /// </summary>
        public static ProductDecision Decide(Product product, DateTime now)
        {
            DateTime today = now.Date;
            bool isInSeason = today > product.SeasonStartDate && today < product.SeasonEndDate;

            return isInSeason && product.Available > 0
                ? ProductDecision.Sell(product)
                : DecideWhenNotSellable(product, now);
        }

        /// <summary>
        /// Compares season dates with the current time of day, unlike <see cref="Decide"/>.
        /// </summary>
        public static ProductDecision DecideWhenNotSellable(Product product, DateTime now)
        {
            if (now.AddDays(product.LeadTime) > product.SeasonEndDate)
            {
                return ProductDecision.NotifyOutOfStock(product, clearStock: true);
            }

            if (product.SeasonStartDate > now)
            {
                return ProductDecision.NotifyOutOfStock(product, clearStock: false);
            }

            return ProductDecision.NotifyDelay(product);
        }
    }
}
