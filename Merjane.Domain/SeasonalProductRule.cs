using Merjane.Domain.Entities;

namespace Merjane.Domain
{
    /// <summary>
    /// Availability rule for <c>SEASONAL</c> products, which are only sold during their season.
    /// </summary>
    /// <remarks>
    /// <see cref="Decide"/> compares today's date with the season dates, whereas <see cref="DecideWhenNotSellable"/> compares
    /// the current date and time. This mix is kept from the original code and leads to these boundary behaviors:
    /// <list type="bullet">
    /// <item><description>The product is not sold on the season start and end days, even when in stock.</description></item>
    /// <item><description>With season dates at midnight, the start day sends a delay notification and keeps the stock (unless replenishment would end after the season), and the end day clears the stock with an out-of-stock notification. A season date with a time of day is compared with the current time instead.</description></item>
    /// <item><description>A missing season date makes every comparison with it false, so the product is never sold.</description></item>
    /// </list>
    /// </remarks>
    public static class SeasonalProductRule
    {
        /// <summary>
        /// Sells one unit when today's date is strictly between the season start and end dates and the product is in stock;
        /// otherwise applies <see cref="DecideWhenNotSellable"/>.
        /// </summary>
        /// <param name="product">Ordered product of type <c>SEASONAL</c>.</param>
        /// <param name="now">Current date and time. Only its date is used for the season check.</param>
        /// <returns>The decision for the product.</returns>
        public static ProductDecision Decide(Product product, DateTime now)
        {
            DateTime today = now.Date;
            bool isInSeason = today > product.SeasonStartDate && today < product.SeasonEndDate;

            return isInSeason && product.Available > 0
                ? ProductDecision.Sell(product)
                : DecideWhenNotSellable(product, now);
        }

        /// <summary>
        /// Decides the outcome for a seasonal product that cannot be sold, comparing season dates with the current time of day.
        /// </summary>
        /// <remarks>
        /// Conditions are checked in this order:
        /// <list type="number">
        /// <item><description>Replenishment (<paramref name="now"/> plus <see cref="Product.LeadTime"/> days) ends after the season end: out-of-stock notification and stock cleared. This also applies once the season is over.</description></item>
        /// <item><description>The season starts after <paramref name="now"/>: out-of-stock notification and stock kept.</description></item>
        /// <item><description>Otherwise: delay notification and stock kept.</description></item>
        /// </list>
        /// Public because the legacy <c>ProductService.HandleSeasonalProduct</c> calls it directly.
        /// </remarks>
        /// <param name="product">Seasonal product that is out of season or out of stock.</param>
        /// <param name="now">Current date and time, compared with the season dates including the time of day.</param>
        /// <returns>An out-of-stock or delay notification decision.</returns>
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
