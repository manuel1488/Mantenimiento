namespace App.Core.DTOs.Shop.Calculation;

public class LineCalculationResult
{
    public decimal BasePriceBeforeSurcharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
}
