using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Shop;

/// <summary>
/// Represents a surcharge configuration for a product-fraction combination.
/// </summary>
[Table("sh_product_partial_surcharges")]
public class ProductPartialSurcharge : BaseEntity<long>
{
    /// <summary>
    /// Reference to the product.
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// Reference to the partial sale fraction.
    /// </summary>
    public int PartialSaleFractionId { get; set; }

    /// <summary>
    /// Surcharge percentage to apply (0-100).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal SurchargePercentage { get; set; }

    /// <summary>
    /// Whether this surcharge configuration is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(PartialSaleFractionId))]
    public virtual PartialSaleFraction PartialSaleFraction { get; set; } = null!;
}
