using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Shop;

/// <summary>
/// Represents a fraction definition for partial sales (e.g., 1/2, 1/4, 1/8).
/// </summary>
[Table("sh_partial_sale_fractions")]
public class PartialSaleFraction : BaseEntity<int>
{
    /// <summary>
    /// Fraction code (e.g., "1/2", "1/4", "1/8").
    /// </summary>
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    /// <summary>
    /// Display name (e.g., "Half", "Quarter", "Eighth").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Numerator of the fraction.
    /// </summary>
    public int Numerator { get; set; }

    /// <summary>
    /// Denominator of the fraction.
    /// </summary>
    public int Denominator { get; set; }

    /// <summary>
    /// Decimal value of the fraction (e.g., 0.5, 0.25, 0.125).
    /// </summary>
    [Column(TypeName = "decimal(10,6)")]
    public decimal FractionValue { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this fraction is active and available for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ProductPartialSurcharge> ProductSurcharges { get; set; } = new List<ProductPartialSurcharge>();
}
