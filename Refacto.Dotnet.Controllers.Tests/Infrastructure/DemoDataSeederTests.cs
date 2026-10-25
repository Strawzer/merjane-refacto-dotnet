using Merjane.Application;
using Merjane.Application.Ports;
using Merjane.Domain.Entities;
using Merjane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Infrastructure
{
    public sealed class DemoDataSeederTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 14, 30, 0);
        private static readonly string[] ProductTypes = ["EXPIRABLE", "NORMAL", "SEASONAL"];

        [Fact]
        public void SeedsOneOrderWithThreeProductsOnlyOnce()
        {
            using AppDbContext ctx = CreateContext();

            long? orderId = DemoDataSeeder.SeedDemoOrder(ctx, Now);
            long? secondRun = DemoDataSeeder.SeedDemoOrder(ctx, Now);

            Order order = ctx.Orders.Include(o => o.Items).Single();
            Assert.Equal(order.Id, orderId);
            Assert.Null(secondRun);
            Assert.Equal(ProductTypes, order.Items!.Select(p => p.Type).Order());
            Assert.Equal(3, ctx.Products.Count());
        }

        [Fact]
        public void SeededOrderSellsOneUnitOfEachProductWithoutNotification()
        {
            using AppDbContext ctx = CreateContext();
            Mock<INotificationService> notifications = new();
            Mock<IClock> clock = new();
            _ = clock.Setup(c => c.Now).Returns(Now);
            EfOrderProcessingStore store = new(ctx);
            OrderProcessingService service = new(store, new ProductDecisionExecutor(new ProductNotificationAdapter(notifications.Object), store), clock.Object);

            long orderId = DemoDataSeeder.SeedDemoOrder(ctx, Now)!.Value;
            _ = service.ProcessOrder(orderId);

            Dictionary<string, int> stock = ctx.Products.ToDictionary(p => p.Type!, p => p.Available);
            Assert.Equal(4, stock["NORMAL"]);
            Assert.Equal(2, stock["SEASONAL"]);
            Assert.Equal(3, stock["EXPIRABLE"]);
            notifications.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("Development", "true", 1)]
        [InlineData("Development", "false", 0)]
        [InlineData("Development", null, 0)]
        [InlineData("Production", "true", 0)]
        public void StartupSeedsOnlyInDevelopmentWhenEnabled(string environment, string? seedSetting, int expectedOrders)
        {
            string databaseName = $"DemoDataSeederTests-{Guid.NewGuid()}";
            using WebApplicationFactory<Program> baseFactory = new();
            using WebApplicationFactory<Program> factory = baseFactory.WithWebHostBuilder(builder =>
            {
                _ = builder.UseEnvironment(environment);
                if (seedSetting is not null)
                {
                    _ = builder.UseSetting("DemoData:Seed", seedSetting);
                }

                _ = builder.ConfigureServices(services =>
                {
                    ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    _ = services.Remove(descriptor);
                    _ = services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                });
            });

            using IServiceScope scope = factory.Services.CreateScope();

            Assert.Equal(expectedOrders, scope.ServiceProvider.GetRequiredService<AppDbContext>().Orders.Count());
        }

        private static AppDbContext CreateContext()
        {
            return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"DemoDataSeederTests-{Guid.NewGuid()}")
                .Options);
        }
    }
}
