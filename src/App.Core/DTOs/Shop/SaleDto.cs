using App.Core.Constants;
using App.Core.DTOs.Shared;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class SaleDto : AuditableDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public DateTime SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal Total { get; set; }
    public App.Core.Enums.Shop.SaleStatus Status { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public SaleType SaleType { get; set; }
    public string? DiscountAuthorizedBy { get; set; }
    
    // Tax Configuration Information
    public decimal TaxRate { get; set; }
    public string TaxCountryCode { get; set; } = null!;
    public string? TaxRegionCode { get; set; }
    public bool TaxIncluded { get; set; }
    public string? TaxDisplayName { get; set; } // e.g., "IVA", "GST", "VAT"
    public DateTime TaxEffectiveDate { get; set; }
    
    // Additional tax details for reporting
    public decimal TaxableAmount { get; set; } // Amount subject to tax (after discount, before tax)
    public decimal NonTaxableAmount { get; set; } // Amount not subject to tax
    
    public List<SaleDetailDto> Details { get; set; } = new();
}