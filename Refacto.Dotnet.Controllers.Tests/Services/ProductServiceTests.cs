using Merjane.Domain.Entities;
using Merjane.Infrastructure.Persistence;
using Moq;
using Moq.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockDbContext = new Mock<AppDbContext>();
            _ = _mockDbContext.Setup(x => x.Products).ReturnsDbSet(Array.Empty<Product>());
            _productService = new ProductService(_mockNotificationService.Object, _mockDbContext.Object);
        }

        [Fact]
        public void NotifyDelay_SendsDelayNotificationAndSavesWithoutChangingStock()
        {
            // GIVEN
            Product product = new()
            {
                LeadTime = 15,
                Available = 0,
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            // WHEN
            _productService.NotifyDelay(product.LeadTime, product);

            // THEN
            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendDelayNotification(product.LeadTime, product.Name), Times.Once());
        }

        [Fact]
        public void HandleSeasonalProduct_ReplenishmentBeyondSeasonEnd_SendsOutOfStockAndClearsStock()
        {
            Product product = Seasonal(available: 4, leadTime: 30, seasonStart: DateTime.Today.AddDays(-5), seasonEnd: DateTime.Today.AddDays(10));

            _productService.HandleSeasonalProduct(product);

            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification(product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void HandleSeasonalProduct_SeasonNotStarted_SendsOutOfStockAndKeepsStock()
        {
            Product product = Seasonal(available: 4, leadTime: 5, seasonStart: DateTime.Today.AddDays(10), seasonEnd: DateTime.Today.AddDays(60));

            _productService.HandleSeasonalProduct(product);

            Assert.Equal(4, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification(product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void HandleSeasonalProduct_ReplenishmentWithinSeason_SendsDelayNotification()
        {
            Product product = Seasonal(available: 0, leadTime: 5, seasonStart: DateTime.Today.AddDays(-5), seasonEnd: DateTime.Today.AddDays(60));

            _productService.HandleSeasonalProduct(product);

            Assert.Equal(0, product.Available);
            Assert.Equal(5, product.LeadTime);
            _mockNotificationService.Verify(service => service.SendDelayNotification(5, product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void HandleExpiredProduct_Expired_SendsExpirationNotificationAndClearsStock()
        {
            DateTime expiryDate = DateTime.Today.AddDays(-1);
            Product product = new() { LeadTime = 15, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = expiryDate };

            _productService.HandleExpiredProduct(product);

            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendExpirationNotification(product.Name!, expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void HandleExpiredProduct_NotExpiredWithStock_DecrementsStock()
        {
            Product product = new() { LeadTime = 15, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = DateTime.Today.AddDays(3) };

            _productService.HandleExpiredProduct(product);

            Assert.Equal(5, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private static Product Seasonal(int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product { LeadTime = leadTime, Available = available, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = seasonStart, SeasonEndDate = seasonEnd };
        }
    }
}
