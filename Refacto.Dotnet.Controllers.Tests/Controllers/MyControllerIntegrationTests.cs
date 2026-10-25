using System.Net;
using Merjane.Application.Ports;
using Merjane.Domain.Entities;
using Merjane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IClock> _mockClock;

        private static readonly DateTime Now = new(2026, 6, 15, 14, 30, 0);
        private static DateTime Today => Now.Date;

        public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockClock = new Mock<IClock>();
            _ = _mockClock.Setup(c => c.Now).Returns(Now);

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(_mockNotificationService.Object);
                    _ = services.AddSingleton(_mockClock.Object);

                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped((_sp) => _mockNotificationService.Object);


                    ServiceProvider sp = services.BuildServiceProvider();
                });
            });

            IServiceScope scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ProcessOrderShouldReturn()
        {
            HttpClient client = _factory.CreateClient();

            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);
            await _context.Products.AddRangeAsync(allProducts);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();

            Assert.Equal($"{{\"id\":{order.Id}}}", await response.Content.ReadAsStringAsync());
            Order? resultOrder = await _context.Orders.FindAsync(order.Id);
            Assert.NotNull(resultOrder);
        }

        [Fact]
        public async Task ProcessOrder_ReturnsOrderIdAsJsonBody()
        {
            Order order = await SeedOrderAsync(Normal(available: 1, leadTime: 15));

            HttpResponseMessage response = await _factory.CreateClient().PostAsync($"/orders/{order.Id}/processOrder", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{{\"id\":{order.Id}}}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ProcessOrder_UnknownOrder_FailsWithServerError()
        {
            HttpResponseMessage response = await _factory.CreateClient().PostAsync("/orders/999/processOrder", null);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task ProcessOrder_EmptyOrder_ReturnsOrderId()
        {
            Order order = await SeedOrderAsync();

            HttpResponseMessage response = await _factory.CreateClient().PostAsync($"/orders/{order.Id}/processOrder", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task NormalProduct_InStock_DecrementsStockByOne()
        {
            Product product = Normal(available: 30, leadTime: 15);

            await ProcessOrderAsync(product);

            Assert.Equal(29, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task NormalProduct_LastUnit_DecrementsStockToZero()
        {
            Product product = Normal(available: 1, leadTime: 15);

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task NormalProduct_OutOfStock_SendsDelayNotificationWithLeadTime()
        {
            Product product = Normal(available: 0, leadTime: 10);

            await ProcessOrderAsync(product);

            Product reloaded = await ReloadAsync(product);
            Assert.Equal(0, reloaded.Available);
            Assert.Equal(10, reloaded.LeadTime);
            _mockNotificationService.Verify(s => s.SendDelayNotification(10, product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task NormalProduct_OutOfStockWithoutLeadTime_DoesNothing()
        {
            Product product = Normal(available: 0, leadTime: 0);

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_InSeasonWithStock_DecrementsStockByOne()
        {
            Product product = Seasonal(available: 30, leadTime: 15, seasonStart: Today.AddDays(-2), seasonEnd: Today.AddDays(58));

            await ProcessOrderAsync(product);

            Assert.Equal(29, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_InSeasonOutOfStock_ReplenishmentWithinSeason_SendsDelayNotification()
        {
            Product product = Seasonal(available: 0, leadTime: 15, seasonStart: Today.AddDays(-2), seasonEnd: Today.AddDays(58));

            await ProcessOrderAsync(product);

            Product reloaded = await ReloadAsync(product);
            Assert.Equal(0, reloaded.Available);
            Assert.Equal(15, reloaded.LeadTime);
            _mockNotificationService.Verify(s => s.SendDelayNotification(15, product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_InSeasonOutOfStock_ReplenishmentBeyondSeasonEnd_SendsOutOfStockNotification()
        {
            Product product = Seasonal(available: 0, leadTime: 90, seasonStart: Today.AddDays(-2), seasonEnd: Today.AddDays(58));

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification(product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_BeforeSeason_SendsOutOfStockNotificationAndKeepsStock()
        {
            Product product = Seasonal(available: 30, leadTime: 15, seasonStart: Today.AddDays(180), seasonEnd: Today.AddDays(240));

            await ProcessOrderAsync(product);

            Assert.Equal(30, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification(product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_AfterSeason_SendsOutOfStockNotificationAndClearsStock()
        {
            Product product = Seasonal(available: 30, leadTime: 15, seasonStart: Today.AddDays(-60), seasonEnd: Today.AddDays(-1));

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification(product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_OnSeasonStartDay_IsNotSold_AndSendsDelayNotification()
        {
            Product product = Seasonal(available: 30, leadTime: 15, seasonStart: Today, seasonEnd: Today.AddDays(58));

            await ProcessOrderAsync(product);

            Assert.Equal(30, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendDelayNotification(15, product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SeasonalProduct_OnSeasonEndDay_IsNotSold_AndClearsStock()
        {
            Product product = Seasonal(available: 30, leadTime: 15, seasonStart: Today.AddDays(-10), seasonEnd: Today);

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification(product.Name!), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExpirableProduct_BeforeExpiration_DecrementsStockByOne()
        {
            Product product = Expirable(available: 30, expiryDate: Today.AddDays(26));

            await ProcessOrderAsync(product);

            Assert.Equal(29, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExpirableProduct_ExpiringLaterToday_IsStillSold()
        {
            Product product = Expirable(available: 30, expiryDate: Today.AddHours(23).AddMinutes(59));

            await ProcessOrderAsync(product);

            Assert.Equal(29, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExpirableProduct_OnExpirationDate_SendsExpirationNotificationAndClearsStock()
        {
            DateTime expiryDate = Today;
            Product product = Expirable(available: 30, expiryDate: expiryDate);

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendExpirationNotification(product.Name!, expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExpirableProduct_AfterExpiration_SendsExpirationNotificationAndClearsStock()
        {
            DateTime expiryDate = Today.AddDays(-2);
            Product product = Expirable(available: 6, expiryDate: expiryDate);

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendExpirationNotification(product.Name!, expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExpirableProduct_OutOfStockBeforeExpiration_SendsExpirationNotification()
        {
            DateTime expiryDate = Today.AddDays(26);
            Product product = Expirable(available: 0, expiryDate: expiryDate);

            await ProcessOrderAsync(product);

            Assert.Equal(0, (await ReloadAsync(product)).Available);
            _mockNotificationService.Verify(s => s.SendExpirationNotification(product.Name!, expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UnknownProductType_IsIgnored()
        {
            Product product = new() { LeadTime = 5, Available = 3, Type = "DIGITAL", Name = "E-book" };

            await ProcessOrderAsync(product);

            Assert.Equal(3, (await ReloadAsync(product)).Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessOrder_WithMixedProducts_AppliesEachProductRuleIndependently()
        {
            List<Product> products = CreateProducts();

            await ProcessOrderAsync(products.ToArray());

            List<int> availableByProduct = await _context.Products.AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => p.Available)
                .ToListAsync();
            Assert.Equal(new[] { 29, 0, 29, 0, 29, 30 }, availableByProduct);
            _mockNotificationService.Verify(s => s.SendDelayNotification(10, "USB Dongle"), Times.Once());
            _mockNotificationService.Verify(s => s.SendExpirationNotification("Milk", products[3].ExpiryDate!.Value), Times.Once());
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification("Grapes"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessOrder_OnlyUpdatesProductsOfTheProcessedOrder()
        {
            Product ordered = Normal(available: 10, leadTime: 5);
            Product notOrdered = Normal(available: 10, leadTime: 5);
            _ = await _context.Products.AddAsync(notOrdered);

            await ProcessOrderAsync(ordered);

            Assert.Equal(9, (await ReloadAsync(ordered)).Available);
            Assert.Equal(10, (await ReloadAsync(notOrdered)).Available);
        }

        [Fact]
        public async Task ProcessOrder_WithSeveralOrders_OnlyProcessesTheRequestedOrder()
        {
            Product cableOfOrderA = new() { LeadTime = 5, Available = 10, Type = "NORMAL", Name = "USB Cable" };
            Product webcamOfOrderA = new() { LeadTime = 3, Available = 0, Type = "NORMAL", Name = "Webcam" };
            Product keyboardOfOrderB = new() { LeadTime = 5, Available = 10, Type = "NORMAL", Name = "Keyboard" };
            Product mouseOfOrderB = new() { LeadTime = 7, Available = 0, Type = "NORMAL", Name = "Mouse" };
            _ = await SeedOrderAsync(cableOfOrderA, webcamOfOrderA);
            Order orderB = await SeedOrderAsync(keyboardOfOrderB, mouseOfOrderB);

            HttpResponseMessage response = await _factory.CreateClient().PostAsync($"/orders/{orderB.Id}/processOrder", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{{\"id\":{orderB.Id}}}", await response.Content.ReadAsStringAsync());
            Assert.Equal(9, (await ReloadAsync(keyboardOfOrderB)).Available);
            Assert.Equal(0, (await ReloadAsync(mouseOfOrderB)).Available);
            Assert.Equal(10, (await ReloadAsync(cableOfOrderA)).Available);
            Assert.Equal(0, (await ReloadAsync(webcamOfOrderA)).Available);
            _mockNotificationService.Verify(s => s.SendDelayNotification(7, "Mouse"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private async Task<Order> SeedOrderAsync(params Product[] products)
        {
            Order order = CreateOrder(new HashSet<Product>(products));
            await _context.Products.AddRangeAsync(products);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return order;
        }

        private async Task ProcessOrderAsync(params Product[] products)
        {
            Order order = await SeedOrderAsync(products);
            HttpResponseMessage response = await _factory.CreateClient().PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();
        }

        private Task<Product> ReloadAsync(Product product)
        {
            return _context.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        }

        private static Product Normal(int available, int leadTime)
        {
            return new Product { LeadTime = leadTime, Available = available, Type = "NORMAL", Name = "USB Cable" };
        }

        private static Product Seasonal(int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product { LeadTime = leadTime, Available = available, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = seasonStart, SeasonEndDate = seasonEnd };
        }

        private static Product Expirable(int available, DateTime expiryDate)
        {
            return new Product { LeadTime = 15, Available = available, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = expiryDate };
        }

        private static Order CreateOrder(HashSet<Product> products)
        {
            return new Order { Items = products };
        }

        private static List<Product> CreateProducts()
        {
            return new List<Product>
            {
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "NORMAL", Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = Now.AddDays(-2) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = Now.AddDays(-2), SeasonEndDate = Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Grapes", SeasonStartDate = Now.AddDays(180), SeasonEndDate = Now.AddDays(240) }
            };
        }
    }
}
