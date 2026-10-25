using Refacto.DotNet.Controllers.Domain;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Tests.Domain
{
    public class ExpirableProductRuleTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 14, 30, 0);
        private static readonly DateTime Today = Now.Date;

        [Fact]
        public void BeforeExpiration_SellsOneUnit()
        {
            ProductDecision decision = ExpirableProductRule.Decide(Expirable(30, Today.AddDays(26)), Now);

            Assert.Equal(29, decision.NewAvailable);
            Assert.Null(decision.Notification);
        }

        [Fact]
        public void ExpiringTomorrow_SellsOneUnit()
        {
            ProductDecision decision = ExpirableProductRule.Decide(Expirable(30, Today.AddDays(1)), Now);

            Assert.Equal(29, decision.NewAvailable);
        }

        [Fact]
        public void ExpiringEarlierToday_IsStillSold_BecauseOnlyTheDateIsCompared()
        {
            ProductDecision decision = ExpirableProductRule.Decide(Expirable(30, Today.AddHours(10)), Now);

            Assert.Equal(29, decision.NewAvailable);
        }

        [Fact]
        public void ExpiringAtMidnightToday_ClearsStockWithExpirationNotification()
        {
            ProductDecision decision = ExpirableProductRule.Decide(Expirable(30, Today), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new ExpirationNotification("Butter", Today), decision.Notification);
        }

        [Fact]
        public void Expired_ClearsStockWithExpirationNotification()
        {
            DateTime expiryDate = Today.AddDays(-2);

            ProductDecision decision = ExpirableProductRule.Decide(Expirable(6, expiryDate), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new ExpirationNotification("Butter", expiryDate), decision.Notification);
        }

        [Fact]
        public void OutOfStockBeforeExpiration_StillSendsExpirationNotification()
        {
            DateTime expiryDate = Today.AddDays(26);

            ProductDecision decision = ExpirableProductRule.Decide(Expirable(0, expiryDate), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new ExpirationNotification("Butter", expiryDate), decision.Notification);
        }

        [Fact]
        public void MissingExpiryDate_Throws()
        {
            Product product = new() { LeadTime = 15, Available = 3, Type = "EXPIRABLE", Name = "Butter" };

            _ = Assert.Throws<InvalidOperationException>(() => ExpirableProductRule.Decide(product, Now));
        }

        [Fact]
        public void WhenNotSellable_ExpiringLaterToday_ComparesTimeOfDay_AndSellsOneUnit()
        {
            ProductDecision decision = ExpirableProductRule.DecideWhenNotSellable(Expirable(30, Today.AddHours(18)), Now);

            Assert.Equal(29, decision.NewAvailable);
            Assert.Null(decision.Notification);
        }

        [Fact]
        public void WhenNotSellable_ExpiredEarlierToday_ClearsStock()
        {
            DateTime expiryDate = Today.AddHours(10);

            ProductDecision decision = ExpirableProductRule.DecideWhenNotSellable(Expirable(30, expiryDate), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new ExpirationNotification("Butter", expiryDate), decision.Notification);
        }

        private static Product Expirable(int available, DateTime expiryDate)
        {
            return new Product { LeadTime = 15, Available = available, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = expiryDate };
        }
    }
}
