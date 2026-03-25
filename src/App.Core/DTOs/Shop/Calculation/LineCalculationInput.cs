namespace App.Core.DTOs.Shop.Calculation;

public class LineCalculationInput
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal SurchargePercentage { get; set; }
    public bool HasCustomTotal { get; set; }
    public decimal? CustomTotal { get; set; }

    /// <summary>Tax rate as decimal fraction (e.g. 0.16 for 16% IVA). Set to 0 for non-taxable.</summary>
    public decimal TaxRate { get; set; }
}
