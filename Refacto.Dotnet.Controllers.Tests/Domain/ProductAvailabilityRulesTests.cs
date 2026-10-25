using Refacto.DotNet.Controllers.Domain;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Tests.Domain
{
    public class ProductAvailabilityRulesTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 14, 30, 0);

        [Theory]
        [InlineData("NORMAL")]
        [InlineData("SEASONAL")]
        [InlineData("EXPIRABLE")]
        public void KnownTypeAvailableToday_SellsOneUnit(string type)
        {
            Product product = new()
            {
                LeadTime = 15,
                Available = 5,
                Type = type,
                Name = "Product",
                ExpiryDate = Now.AddDays(10),
                SeasonStartDate = Now.AddDays(-10),
                SeasonEndDate = Now.AddDays(10)
            };

            Assert.Equal(4, ProductAvailabilityRules.Decide(product, Now).NewAvailable);
        }

        [Theory]
        [InlineData("DIGITAL")]
        [InlineData("normal")]
        [InlineData(null)]
        public void UnknownType_IsLeftUntouched(string? type)
        {
            Product product = new() { LeadTime = 15, Available = 0, Type = type, Name = "Product" };

            ProductDecision decision = ProductAvailabilityRules.Decide(product, Now);

            Assert.Equal(ProductDecision.None, decision);
            Assert.False(decision.RequiresSave);
        }
    }
}
