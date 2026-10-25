using Refacto.DotNet.Controllers.Domain;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Tests.Domain
{
    public class NormalProductRuleTests
    {
        [Fact]
        public void InStock_SellsOneUnit()
        {
            ProductDecision decision = NormalProductRule.Decide(Normal(available: 30, leadTime: 15));

            Assert.Equal(29, decision.NewAvailable);
            Assert.Null(decision.Notification);
        }

        [Fact]
        public void LastUnit_SellsDownToZero()
        {
            ProductDecision decision = NormalProductRule.Decide(Normal(available: 1, leadTime: 15));

            Assert.Equal(0, decision.NewAvailable);
            Assert.Null(decision.Notification);
        }

        [Fact]
        public void OutOfStock_NotifiesDelayWithLeadTime_AndKeepsStock()
        {
            ProductDecision decision = NormalProductRule.Decide(Normal(available: 0, leadTime: 10));

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new DelayNotification("USB Cable", 10), decision.Notification);
        }

        [Fact]
        public void OutOfStockWithoutLeadTime_DoesNothing()
        {
            ProductDecision decision = NormalProductRule.Decide(Normal(available: 0, leadTime: 0));

            Assert.Equal(ProductDecision.None, decision);
            Assert.False(decision.RequiresSave);
        }

        private static Product Normal(int available, int leadTime)
        {
            return new Product { LeadTime = leadTime, Available = available, Type = "NORMAL", Name = "USB Cable" };
        }
    }
}
