namespace App.Core.DTOs.Shop.Calculation;

public class DocumentCalculationResult
{
    public decimal Subtotal { get; set; }
    public decimal ItemDiscountAmount { get; set; }
    public decimal GlobalDiscountAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal PreRoundingTotal { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal Total { get; set; }
}
