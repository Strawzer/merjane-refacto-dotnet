using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Merjane.Domain.Entities
{
    /// <summary>
    /// A customer order, mapped to the <c>orders</c> table.
    /// </summary>
    /// <remarks>
    /// An order carries no quantities: each product in <see cref="Items"/> stands for one ordered unit.
    /// </remarks>
    [Table("orders")]
    public class Order
    {
        /// <summary>Database-generated identifier, used in the processing route.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// Ordered products, stored as a many-to-many relationship without an inverse navigation on <see cref="Product"/>.
        /// </summary>
        /// <value>
        /// <see langword="null"/> until loaded; order processing loads the order together with its products.
        /// </value>
        public ICollection<Product>? Items { get; set; }
    }
}
