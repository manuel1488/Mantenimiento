namespace App.Core.DTOs.Shop.Calculation;

public class WholesaleDiscountResult
{
    public decimal DiscountPercentage { get; set; }
    public decimal? FixedDiscountAmountPerUnit { get; set; }
    public bool IsFixedPrice => FixedDiscountAmountPerUnit.HasValue;
    public bool HasDiscount => DiscountPercentage > 0 || IsFixedPrice;

    public decimal? GetFixedLineDiscount(decimal quantity) =>
        FixedDiscountAmountPerUnit.HasValue ? FixedDiscountAmountPerUnit.Value * quantity : null;
}
