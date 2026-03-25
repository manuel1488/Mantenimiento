namespace App.Core.DTOs.Shop.Calculation;

public class DocumentCalculationInput
{
    public List<DocumentLineInput> Lines { get; set; } = [];
    public decimal GlobalDiscountPercentage { get; set; }
    public decimal TaxRate { get; set; }
    public bool ApplyRounding { get; set; }
}

public class DocumentLineInput
{
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsTaxable { get; set; }

    /// <summary>Pre-computed per-line tax amount using Round(TaxBase × TaxRate, 2).</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Pre-computed tax base = Round(Qty × UnitPrice, 2) - DiscountAmount.</summary>
    public decimal TaxBase { get; set; }
}
