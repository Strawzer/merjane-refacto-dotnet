using Merjane.Domain.Entities;

namespace Merjane.Infrastructure.Persistence
{
    /// <summary>
    /// Seeds a demonstration order for manual testing through Swagger.
    /// </summary>
    /// <remarks>
    /// The host calls it only in the Development environment when <c>DemoData:Seed</c> is <see langword="true"/>.
    /// The data lives in the in-memory database and disappears when the application stops.
    /// </remarks>
    public static class DemoDataSeeder
    {
        /// <summary>
        /// Adds one order containing a sellable <c>NORMAL</c>, <c>SEASONAL</c> and <c>EXPIRABLE</c> product,
        /// unless the database already contains an order.
        /// </summary>
        /// <param name="context">Context of the database to seed.</param>
        /// <param name="now">Current date and time; seasonal and expiry dates are relative to it.</param>
        /// <returns>The identifier of the seeded order, or <see langword="null"/> when an order already exists.</returns>
        public static long? SeedDemoOrder(AppDbContext context, DateTime now)
        {
            if (context.Orders.Any())
            {
                return null;
            }

            DateTime today = now.Date;
            Order order = new()
            {
                Items = new List<Product>
                {
                    new() { Type = "NORMAL", Name = "USB Cable", Available = 5, LeadTime = 15 },
                    new() { Type = "SEASONAL", Name = "Watermelon", Available = 3, LeadTime = 15, SeasonStartDate = today.AddDays(-10), SeasonEndDate = today.AddDays(20) },
                    new() { Type = "EXPIRABLE", Name = "Butter", Available = 4, LeadTime = 15, ExpiryDate = today.AddDays(30) },
                },
            };

            _ = context.Orders.Add(order);
            _ = context.SaveChanges();
            return order.Id;
        }
    }
}
