namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for product partial surcharge configuration.
/// </summary>
public class ProductPartialSurchargeDto
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public int PartialSaleFractionId { get; set; }

    /// <summary>
    /// Fraction code (e.g., "1/2", "1/4").
    /// </summary>
    public string FractionCode { get; set; } = null!;

    /// <summary>
    /// Fraction display name.
    /// </summary>
    public string FractionName { get; set; } = null!;

    /// <summary>
    /// Decimal value of the fraction.
    /// </summary>
    public decimal FractionValue { get; set; }

    /// <summary>
    /// Surcharge percentage (0-100).
    /// </summary>
    public decimal SurchargePercentage { get; set; }

    /// <summary>
    /// Whether this configuration is active.
    /// </summary>
    public bool IsActive { get; set; }
}
