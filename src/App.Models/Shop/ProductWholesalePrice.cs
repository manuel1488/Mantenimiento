using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Shop;

/// <summary>
/// Represents a wholesale discount configuration for a product-tier combination.
/// </summary>
[Table("sh_product_wholesale_prices")]
public class ProductWholesalePrice : BaseEntity<long>
{
    /// <summary>
    /// Reference to the product.
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// Reference to the wholesale tier.
    /// </summary>
    public int WholesaleTierId { get; set; }

    /// <summary>
    /// Minimum quantity (in product's unit of measure) to qualify for this tier.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal MinQuantity { get; set; }

    /// <summary>
    /// Discount percentage to apply (0-100) over the product's base price.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Whether this wholesale discount configuration is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(WholesaleTierId))]
    public virtual WholesaleTier WholesaleTier { get; set; } = null!;
}
