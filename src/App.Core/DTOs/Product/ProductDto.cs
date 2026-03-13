using App.Core.DTOs.Shared;
using App.Core.DTOs.Shop;

namespace App.Core.DTOs.Product;

public class ProductDto : AuditableDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string? Barcode { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Brand { get; set; } = null!;
    public int UnitMeasureId { get; set; }
    public string UnitMeasureName { get; set; } = null!;
    public string UnitMeasureCode { get; set; } = null!;
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsActive { get; set; }
    public decimal Content { get; set; }
    public bool IsPartialSaleAllowed { get; set; }
    public bool AllowCustomPricing { get; set; }
    public bool RequiresInventory { get; set; }
    public int? MexicoProductServiceId { get; set; }
    public string? MexicoProductServiceCode { get; set; }
    public string? MexicoProductServiceDescription { get; set; }
    public List<ProductWholesalePriceDto> WholesalePrices { get; set; } = [];
}