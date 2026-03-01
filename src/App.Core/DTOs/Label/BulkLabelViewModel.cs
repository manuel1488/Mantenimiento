namespace App.Core.DTOs.Label;

/// <summary>
/// View model for the BulkProductLabel Razor template (62mm DK-2205 label).
/// </summary>
public class BulkLabelViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogoBase64 { get; set; }

    /// <summary>GS1-128 barcode image as PNG Base64.</summary>
    public string BarcodeBase64 { get; set; } = string.Empty;

    /// <summary>Human-readable representation of the barcode data.</summary>
    public string BarcodeHumanReadable { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string UnitMeasureCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime LabelDate { get; set; }

    /// <summary>Label width in millimeters. DK-2205 = 62mm.</summary>
    public int LabelWidthMm { get; set; } = 62;

    /// <summary>Label height in millimeters. DK-2205 continuous roll.</summary>
    public int LabelHeightMm { get; set; } = 50;
}
