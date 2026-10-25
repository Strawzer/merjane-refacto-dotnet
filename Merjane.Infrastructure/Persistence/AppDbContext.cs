using Merjane.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Merjane.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core context holding orders and products.
    /// </summary>
    /// <remarks>
    /// The model is kept as in the original code: <see cref="Product"/> maps to the <c>products</c> table and orders
    /// reference their products through a many-to-many relationship.
    /// </remarks>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Creates a context with the configured provider, as registered by
        /// <see cref="InfrastructureServiceCollectionExtensions.AddInfrastructure"/>.
        /// </summary>
        /// <param name="options">Context options, including the database provider.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Creates a context without a configured provider.
        /// </summary>
        /// <remarks>
        /// Kept so that tests can mock the context; such an instance cannot query or save.
        /// </remarks>
        public AppDbContext() : base()
        {
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Configures <see cref="Order.Items"/> as a many-to-many relationship without an inverse navigation on <see cref="Product"/>.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship between Order and Product
            _ = modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithMany(); // Since Product doesn't have a collection for Order

        }


        /// <summary>
        /// Orders. Virtual so that tests can mock it.
        /// </summary>
        public virtual DbSet<Order> Orders { get; set; }

        /// <summary>
        /// Products. Virtual so that tests can mock it.
        /// </summary>
        public virtual DbSet<Product> Products { get; set; }

    }
}
