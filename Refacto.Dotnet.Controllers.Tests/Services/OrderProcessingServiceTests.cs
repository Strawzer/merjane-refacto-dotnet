using Microsoft.EntityFrameworkCore;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public sealed class OrderProcessingServiceTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 6, 15, 14, 30, 0);
        private static readonly DateTime Today = Now.Date;

        private readonly Mock<INotificationService> _mockNotificationService = new();
        private readonly AppDbContext _ctx;
        private readonly OrderProcessingService _service;

        public OrderProcessingServiceTests()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"OrderProcessingServiceTests-{Guid.NewGuid()}")
                .Options;
            _ctx = new AppDbContext(options);
            Mock<IClock> clock = new();
            _ = clock.Setup(c => c.Now).Returns(Now);
            _service = new OrderProcessingService(_ctx, new ProductDecisionExecutor(_mockNotificationService.Object, _ctx), clock.Object);
        }

        [Fact]
        public void UnknownOrder_ThrowsOrderNotFound()
        {
            OrderNotFoundException exception = Assert.Throws<OrderNotFoundException>(() => _service.ProcessOrder(42));

            Assert.Equal(42, exception.OrderId);
        }

        [Fact]
        public void ReturnsProcessedOrderId()
        {
            Order order = Seed(Normal("USB Cable", available: 3));

            Assert.Equal(order.Id, _service.ProcessOrder(order.Id));
        }

        [Fact]
        public void AppliesEachRuleWithTheInjectedClock()
        {
            Product expiringEarlierToday = Expirable("Yogurt", available: 5, expiryDate: Today.AddHours(10));
            Product seasonStartingToday = new() { LeadTime = 15, Available = 8, Type = "SEASONAL", Name = "Melon", SeasonStartDate = Today, SeasonEndDate = Today.AddDays(30) };
            Order order = Seed(expiringEarlierToday, seasonStartingToday);

            _ = _service.ProcessOrder(order.Id);

            Assert.Equal(4, Reload(expiringEarlierToday).Available);
            Assert.Equal(8, Reload(seasonStartingToday).Available);
            _mockNotificationService.Verify(s => s.SendDelayNotification(15, "Melon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void NotificationFailure_KeepsEarlierProductsSaved_AndLeavesFailingProductUnchanged()
        {
            Product normal = Normal("USB Cable", available: 3);
            Product expired = Expirable("Milk", available: 6, expiryDate: Today.AddDays(-1));
            Order order = Seed(normal, expired);
            _ = _mockNotificationService
                .Setup(s => s.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("Notification failed"));

            _ = Assert.Throws<InvalidOperationException>(() => _service.ProcessOrder(order.Id));

            Assert.Equal(2, Reload(normal).Available);
            Assert.Equal(6, Reload(expired).Available);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        private Order Seed(params Product[] products)
        {
            Order order = new() { Items = products.ToList() };
            _ = _ctx.Orders.Add(order);
            _ = _ctx.SaveChanges();
            _ctx.ChangeTracker.Clear();
            return order;
        }

        private Product Reload(Product product)
        {
            return _ctx.Products.AsNoTracking().Single(p => p.Id == product.Id);
        }

        private static Product Normal(string name, int available)
        {
            return new Product { LeadTime = 10, Available = available, Type = "NORMAL", Name = name };
        }

        private static Product Expirable(string name, int available, DateTime expiryDate)
        {
            return new Product { LeadTime = 15, Available = available, Type = "EXPIRABLE", Name = name, ExpiryDate = expiryDate };
        }
    }
}
