namespace App.Core.DTOs.Product;

public class ProductBulkLoadRecord
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal Content { get; set; } = 1;
    public string UnitMeasureCode { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MexicoProductServiceCode { get; set; }
    public bool AllowPartialSale { get; set; } = false;
    public decimal QuantityStep { get; set; } = 1;
    public bool AllowLabeling { get; set; } = false;
    public bool AllowCustomPricing { get; set; } = false;

    /// <summary>
    /// Wholesale pricing data parsed from dynamic columns.
    /// Key: Tier name, Value: (MinQuantity, DiscountPercentage, FixedPrice)
    /// FixedPrice is null when using percentage mode; DiscountPercentage is 0 when using fixed-price mode.
    /// </summary>
    public Dictionary<string, (decimal MinQuantity, decimal DiscountPercentage, decimal? FixedPrice)> WholesalePrices { get; set; } = new();
}