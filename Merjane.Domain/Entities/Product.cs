using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Merjane.Domain.Entities
{
    /// <summary>
    /// A product that can be ordered, mapped to the <c>products</c> table.
    /// </summary>
    /// <remarks>
    /// The persistence shape (properties, column names and data annotations) is kept from the original exercise.
    /// Which properties matter depends on <see cref="Type"/>; the availability rules do not validate them.
    /// </remarks>
    [Table("products")]
    public class Product
    {
        /// <summary>Database-generated identifier.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// Replenishment lead time in days, sent in delay notifications and used by the seasonal rule to decide
        /// whether replenishment would end after the season.
        /// </summary>
        [Column("lead_time")]
        public int LeadTime { get; set; }

        /// <summary>
        /// Units in stock. Selling an ordered product removes exactly one unit; no rule sells when it is zero or less.
        /// </summary>
        [Column("available")]
        public int Available { get; set; }

        /// <summary>
        /// Product category selecting the availability rule: <c>NORMAL</c>, <c>SEASONAL</c> or <c>EXPIRABLE</c>.
        /// The match is exact and case-sensitive; any other value, including <see langword="null"/>, leaves the product untouched.
        /// </summary>
        [Column("type")]
        public string? Type { get; set; }

        /// <summary>Product name sent in customer notifications. It is not validated.</summary>
        [Column("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Expiry date and time of an <c>EXPIRABLE</c> product. Processing such a product without an expiry date fails.
        /// </summary>
        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Season start of a <c>SEASONAL</c> product. The product is sold only when today's date is strictly after this value.
        /// </summary>
        [Column("season_start_date")]
        public DateTime? SeasonStartDate { get; set; }

        /// <summary>
        /// Season end of a <c>SEASONAL</c> product. The product is sold only when today's date is strictly before this value,
        /// and replenishment ending after it clears the stock.
        /// </summary>
        [Column("season_end_date")]
        public DateTime? SeasonEndDate { get; set; }
    }
}
