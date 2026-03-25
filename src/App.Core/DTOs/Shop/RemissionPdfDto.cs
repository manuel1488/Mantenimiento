namespace App.Core.DTOs.Shop;

public class RemissionPdfDto
{
    public string RemissionNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string? CustomerLegalName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerTaxId { get; set; }
    public DateTime RemissionDate { get; set; }
    public string LocationName { get; set; } = null!;
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public List<RemissionDetailDto> Details { get; set; } = [];

    // Company info
    public string CompanyName { get; set; } = null!;
    public string? LogoBase64 { get; set; }
    public string CurrencySymbol { get; set; } = "$";
}
