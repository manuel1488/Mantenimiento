namespace App.Core.DTOs.Shop.Calculation;

public class LineCalculationResult
{
    public decimal BasePriceBeforeSurcharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    /// <summary>Round(Quantity × UnitPrice, 2) — CFDI Concepto.Importe.</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>GrossAmount - DiscountAmount (rounded) — CFDI Concepto.Impuestos.Traslado.Base.</summary>
    public decimal TaxBase { get; set; }

    /// <summary>Round(TaxBase × TaxRate, 2) — CFDI Concepto.Impuestos.Traslado.Importe.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Tax rate used for this line (0 if non-taxable).</summary>
    public decimal TaxRate { get; set; }
}
