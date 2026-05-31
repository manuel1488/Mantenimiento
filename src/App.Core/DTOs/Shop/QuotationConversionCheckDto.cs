namespace App.Core.DTOs.Shop;

/// <summary>
/// Report of discrepancies an operator should be aware of when converting a
/// quotation to a sale. All values are informational — the converted sale always
/// honors the locked quoted prices and total; this surfaces what has changed since
/// the quotation was created.
/// </summary>
public class QuotationConversionCheckDto
{
    /// <summary>Products whose current catalog price differs from the quoted price.</summary>
    public List<QuotationPriceChangeDto> PriceChanges { get; set; } = new();

    /// <summary>
    /// True when rounding is enabled. The conversion intentionally does NOT round —
    /// it reproduces the exact quoted total — so a direct sale of the same items
    /// could differ by the rounding adjustment.
    /// </summary>
    public bool RoundingEnabled { get; set; }

    /// <summary>True when the current effective tax rate differs from the quoted rate.</summary>
    public bool TaxRateChanged { get; set; }

    public decimal QuotedTaxRate { get; set; }
    public decimal CurrentTaxRate { get; set; }

    /// <summary>True when there is anything worth showing the operator.</summary>
    public bool HasWarnings =>
        PriceChanges.Count > 0 || RoundingEnabled || TaxRateChanged;
}

public class QuotationPriceChangeDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Unit price locked in the quotation (will be honored in the sale).</summary>
    public decimal QuotedPrice { get; set; }

    /// <summary>Current catalog unit price.</summary>
    public decimal CurrentPrice { get; set; }
}
