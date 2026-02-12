namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for wholesale tier data.
/// </summary>
public class WholesaleTierDto
{
    public int Id { get; set; }

    /// <summary>
    /// Display name of the tier (e.g., "Medio Mayoreo", "Mayoreo").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this tier is active.
    /// </summary>
    public bool IsActive { get; set; }
}
