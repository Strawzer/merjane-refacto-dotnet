using Merjane.Domain;
using Merjane.Domain.Entities;

namespace Refacto.DotNet.Controllers.Tests.Domain
{
    public class SeasonalProductRuleTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 14, 30, 0);
        private static readonly DateTime Today = Now.Date;

        [Fact]
        public void InSeasonWithStock_SellsOneUnit()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddDays(-2), Today.AddDays(58)), Now);

            Assert.Equal(29, decision.NewAvailable);
            Assert.Null(decision.Notification);
        }

        [Fact]
        public void SeasonStartedYesterday_SellsOneUnit()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddDays(-1), Today.AddDays(58)), Now);

            Assert.Equal(29, decision.NewAvailable);
        }

        [Fact]
        public void SeasonStartsToday_IsNotSold_AndNotifiesDelay()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today, Today.AddDays(58)), Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new DelayNotification("Watermelon", 15), decision.Notification);
        }

        [Fact]
        public void SeasonStartsLaterToday_IsNotSold_AndNotifiesOutOfStockKeepingStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddHours(18), Today.AddDays(58)), Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        [Fact]
        public void SeasonStartedEarlierToday_IsNotSold_AndComparesTimeOfDay_ToNotifyDelay()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddHours(10), Today.AddDays(58)), Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new DelayNotification("Watermelon", 15), decision.Notification);
            Assert.True(decision.RequiresSave);
        }

        [Theory]
        [InlineData(null, 58)]
        [InlineData(-2, null)]
        [InlineData(null, null)]
        public void MissingSeasonDate_IsNotSold_AndNotifiesDelay(int? seasonStartOffsetDays, int? seasonEndOffsetDays)
        {
            Product product = new()
            {
                LeadTime = 15,
                Available = 30,
                Type = "SEASONAL",
                Name = "Watermelon",
                SeasonStartDate = seasonStartOffsetDays is int startOffset ? Today.AddDays(startOffset) : null,
                SeasonEndDate = seasonEndOffsetDays is int endOffset ? Today.AddDays(endOffset) : null
            };

            ProductDecision decision = SeasonalProductRule.Decide(product, Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new DelayNotification("Watermelon", 15), decision.Notification);
            Assert.True(decision.RequiresSave);
        }

        [Fact]
        public void MissingSeasonEnd_BeforeSeasonStart_NotifiesOutOfStockAndKeepsStock()
        {
            Product product = new() { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = Today.AddDays(10) };

            ProductDecision decision = SeasonalProductRule.Decide(product, Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        [Fact]
        public void SeasonEndsTomorrow_SellsOneUnit()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 0, Today.AddDays(-10), Today.AddDays(1)), Now);

            Assert.Equal(29, decision.NewAvailable);
        }

        [Fact]
        public void SeasonEndsToday_IsNotSold_AndClearsStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddDays(-10), Today), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        [Fact]
        public void InSeasonOutOfStock_ReplenishmentWithinSeason_NotifiesDelay()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(0, 15, Today.AddDays(-2), Today.AddDays(58)), Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new DelayNotification("Watermelon", 15), decision.Notification);
        }

        [Fact]
        public void InSeasonOutOfStock_ReplenishmentBeyondSeasonEnd_NotifiesOutOfStockAndClearsStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(0, 90, Today.AddDays(-2), Today.AddDays(58)), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        [Fact]
        public void ReplenishmentEndingExactlyAtSeasonEnd_NotifiesDelay()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(0, 10, Today.AddDays(-2), Now.AddDays(10)), Now);

            Assert.Equal(new DelayNotification("Watermelon", 10), decision.Notification);
        }

        [Fact]
        public void ReplenishmentEndingLaterOnSeasonEndDay_UsesTimeOfDay_AndNotifiesOutOfStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(0, 10, Today.AddDays(-2), Today.AddDays(10)), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        [Fact]
        public void BeforeSeason_NotifiesOutOfStockAndKeepsStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddDays(180), Today.AddDays(240)), Now);

            Assert.Null(decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
            Assert.True(decision.RequiresSave);
        }

        [Fact]
        public void BeforeSeason_ReplenishmentBeyondSeasonEnd_ClearsStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 30, Today.AddDays(5), Today.AddDays(20)), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        [Fact]
        public void AfterSeason_NotifiesOutOfStockAndClearsStock()
        {
            ProductDecision decision = SeasonalProductRule.Decide(Seasonal(30, 15, Today.AddDays(-60), Today.AddDays(-1)), Now);

            Assert.Equal(0, decision.NewAvailable);
            Assert.Equal(new OutOfStockNotification("Watermelon"), decision.Notification);
        }

        private static Product Seasonal(int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product { LeadTime = leadTime, Available = available, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = seasonStart, SeasonEndDate = seasonEnd };
        }
    }
}
