using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for creating a new partial sale fraction.
/// </summary>
public class CreatePartialSaleFractionDto
{
    /// <summary>
    /// Fraction code (e.g., "1/2", "1/4", "1/8").
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = null!;

    /// <summary>
    /// Display name (e.g., "Mitad", "Cuarto", "Octavo").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Numerator of the fraction.
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int Numerator { get; set; }

    /// <summary>
    /// Denominator of the fraction.
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int Denominator { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this fraction is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
