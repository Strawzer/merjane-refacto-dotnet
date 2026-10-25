using Merjane.Application.Ports;
using Merjane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Merjane.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            _ = services.AddSingleton<IClock, SystemClock>();
            _ = services.AddDbContext<AppDbContext>(options =>
            {
                _ = options.UseInMemoryDatabase($"InMemoryDb");
            });
            _ = services.AddScoped<IOrderProcessingStore, EfOrderProcessingStore>();
            return services;
        }
    }
}
