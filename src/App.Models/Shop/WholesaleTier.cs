using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Shop;

/// <summary>
/// Represents a wholesale pricing tier (e.g., "Medio Mayoreo", "Mayoreo").
/// </summary>
[Table("sh_wholesale_tiers")]
public class WholesaleTier : BaseEntity<int>
{
    /// <summary>
    /// Display name of the tier.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this tier is active and available for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ProductWholesalePrice> ProductWholesalePrices { get; set; } = new List<ProductWholesalePrice>();
}
