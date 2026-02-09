namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for partial sale fraction data.
/// </summary>
public class PartialSaleFractionDto
{
    public int Id { get; set; }

    /// <summary>
    /// Fraction code (e.g., "1/2", "1/4", "1/8").
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Display name (e.g., "Half", "Quarter", "Eighth").
    /// </summary>
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
    public decimal FractionValue { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this fraction is active.
    /// </summary>
    public bool IsActive { get; set; }
}
