namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO containing the result of a fractional price calculation.
/// </summary>
public class FractionalPriceCalculationDto
{
    public long ProductId { get; set; }

    /// <summary>
    /// Base price per individual unit (e.g., price per liter).
    /// </summary>
    public decimal BaseUnitPrice { get; set; }

    /// <summary>
    /// Quantity being sold.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Selected fraction ID (if a fraction was selected).
    /// </summary>
    public int? FractionId { get; set; }

    /// <summary>
    /// Selected fraction code (e.g., "1/2", "1/4").
    /// </summary>
    public string? FractionCode { get; set; }

    /// <summary>
    /// Surcharge percentage applied.
    /// </summary>
    public decimal SurchargePercentage { get; set; }

    /// <summary>
    /// Subtotal before surcharge (BaseUnitPrice × Quantity).
    /// </summary>
    public decimal BasePriceBeforeSurcharge { get; set; }

    /// <summary>
    /// Surcharge amount in currency.
    /// </summary>
    public decimal SurchargeAmount { get; set; }

    /// <summary>
    /// Final price including surcharge.
    /// </summary>
    public decimal FinalPrice { get; set; }
}
