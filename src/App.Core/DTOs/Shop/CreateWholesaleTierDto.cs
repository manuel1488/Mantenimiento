using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for creating a new wholesale tier.
/// </summary>
public class CreateWholesaleTierDto
{
    /// <summary>
    /// Display name of the tier.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this tier is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
