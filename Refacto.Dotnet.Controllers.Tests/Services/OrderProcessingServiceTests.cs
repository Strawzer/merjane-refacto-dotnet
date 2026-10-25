using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        private readonly FailingSaveInterceptor _failingSave = new();
        private readonly AppDbContext _ctx;
        private readonly OrderProcessingService _service;

        public OrderProcessingServiceTests()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"OrderProcessingServiceTests-{Guid.NewGuid()}")
                .AddInterceptors(_failingSave)
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
            AssertProcessingOrder(order, "USB Cable", "Milk");
            _ = _mockNotificationService
                .Setup(s => s.SendExpirationNotification(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("Notification failed"));

            _ = Assert.Throws<InvalidOperationException>(() => _service.ProcessOrder(order.Id));

            Assert.Equal(2, Reload(normal).Available);
            Assert.Equal(6, Reload(expired).Available);
        }

        [Fact]
        public void MissingExpiryDate_StopsProcessing_KeepingEarlierProductsSavedAndLaterOnesUntouched()
        {
            Product first = Normal("USB Cable", available: 3);
            Product withoutExpiryDate = new() { LeadTime = 15, Available = 3, Type = "EXPIRABLE", Name = "Milk" };
            Product last = Normal("Mouse", available: 3);
            Order order = Seed(first, withoutExpiryDate, last);
            AssertProcessingOrder(order, "USB Cable", "Milk", "Mouse");

            _ = Assert.Throws<InvalidOperationException>(() => _service.ProcessOrder(order.Id));

            Assert.Equal(2, Reload(first).Available);
            Assert.Equal(3, Reload(withoutExpiryDate).Available);
            Assert.Equal(3, Reload(last).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void SaveFailure_AfterNotification_Propagates_AndOnlyKeepsEarlierProductsSaved()
        {
            DateTime expiryDate = Today.AddDays(-1);
            Product normal = Normal("USB Cable", available: 3);
            Product expired = Expirable("Milk", available: 6, expiryDate: expiryDate);
            Order order = Seed(normal, expired);
            AssertProcessingOrder(order, "USB Cable", "Milk");
            _failingSave.FailWhenSavingProduct = "Milk";

            _ = Assert.Throws<DbUpdateException>(() => _service.ProcessOrder(order.Id));

            Assert.Equal(2, Reload(normal).Available);
            Assert.Equal(6, Reload(expired).Available);
            _mockNotificationService.Verify(s => s.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        private void AssertProcessingOrder(Order order, params string[] expectedProductNames)
        {
            List<string?> productNames = _ctx.Orders
                .Include(o => o.Items)
                .Single(o => o.Id == order.Id)
                .Items!
                .Select(p => p.Name)
                .ToList();
            _ctx.ChangeTracker.Clear();

            Assert.Equal(expectedProductNames, productNames);
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

        private sealed class FailingSaveInterceptor : SaveChangesInterceptor
        {
            public string? FailWhenSavingProduct { get; set; }

            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                bool savesFailingProduct = eventData.Context!.ChangeTracker.Entries<Product>()
                    .Any(e => e.State == EntityState.Modified && e.Entity.Name == FailWhenSavingProduct);

                return savesFailingProduct ? throw new DbUpdateException("Save failed") : result;
            }
        }
    }
}
