using Merjane.Application.Ports;
using Merjane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Merjane.Infrastructure
{
    /// <summary>
    /// Registers the infrastructure adapters of the application ports.
    /// </summary>
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the clock, the EF Core context and the order processing store.
        /// </summary>
        /// <remarks>
        /// Registrations:
        /// <list type="bullet">
        /// <item><description><see cref="IClock"/> as a singleton <see cref="SystemClock"/>.</description></item>
        /// <item><description><see cref="AppDbContext"/> as scoped, on the EF Core in-memory database <c>InMemoryDb</c>, which starts empty and is lost when the process stops.</description></item>
        /// <item><description><see cref="IOrderProcessingStore"/> as a scoped <see cref="EfOrderProcessingStore"/>, sharing the request's context.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="services">Service collection of the host.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            _ = services.AddSingleton<IClock, SystemClock>();
            _ = services.AddDbContext<AppDbContext>(options =>
            {
                _ = options.UseInMemoryDatabase("InMemoryDb");
            });
            _ = services.AddScoped<IOrderProcessingStore, EfOrderProcessingStore>();
            return services;
        }
    }
}
