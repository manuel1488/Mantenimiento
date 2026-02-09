namespace App.Core.DTOs.Shop;

public class SaleDetailDto
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public bool IsCustomPrice { get; set; }

    /// <summary>
    /// Partial sale fraction ID (if applicable).
    /// </summary>
    public int? PartialSaleFractionId { get; set; }

    /// <summary>
    /// Partial sale fraction code (e.g., "1/2", "1/4").
    /// </summary>
    public string? PartialSaleFractionCode { get; set; }

    /// <summary>
    /// Partial sale fraction display name.
    /// </summary>
    public string? PartialSaleFractionName { get; set; }

    /// <summary>
    /// Surcharge percentage applied for partial sale.
    /// </summary>
    public decimal SurchargePercentage { get; set; }

    /// <summary>
    /// Surcharge amount in currency.
    /// </summary>
    public decimal SurchargeAmount { get; set; }

    /// <summary>
    /// Base price before surcharge was applied.
    /// </summary>
    public decimal BasePriceBeforeSurcharge { get; set; }
}