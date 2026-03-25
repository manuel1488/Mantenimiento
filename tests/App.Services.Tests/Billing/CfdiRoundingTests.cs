using App.Core.Models.Cfdi.V40;
using NUnit.Framework;

namespace App.Services.Tests.Billing;

/// <summary>
/// Tests for CFDI rounding concepto behavior.
/// When sale.Total > cfdiTotal (positive adjustment from POS rounding or
/// precision difference), a non-taxable "Ajuste por redondeo" concepto
/// is added to make the CFDI total match.
/// </summary>
[TestFixture]
public class CfdiRoundingTests
{
    #region Helpers — simulate BuildComprobante logic

    /// <summary>Line-level: 6-decimal precision (matching PricingCalculationService).</summary>
    private static decimal LineImporte(decimal qty, decimal unitPrice)
        => Math.Round(qty * unitPrice, 6);

    private static decimal LineTaxImporte(decimal taxBase, decimal taxRate)
        => Math.Round(taxBase * taxRate, 6);

    /// <summary>
    /// Simulates BuildComprobante + rounding adjustment.
    /// Returns (conceptos, cfdiSubTotal, cfdiDescuento, cfdiTotalImpuestos, cfdiTotal).
    /// </summary>
    private static (List<Concepto> Conceptos, decimal SubTotal, decimal Descuento,
        decimal TotalImpuestos, decimal Total)
        SimulateBuildComprobante(
            (decimal Qty, decimal UnitPrice, decimal Discount, decimal TaxRate)[] lines,
            decimal saleTotal)
    {
        // Build conceptos with 6-decimal line precision
        var conceptos = lines.Select(l =>
        {
            var importe = LineImporte(l.Qty, l.UnitPrice);
            var discount = Math.Round(l.Discount, 6);
            var taxBase = importe - discount;
            var taxImporte = l.TaxRate > 0 ? LineTaxImporte(taxBase, l.TaxRate) : 0m;

            return new Concepto
            {
                ClaveProdServ = "01010101",
                Cantidad = l.Qty,
                ClaveUnidad = "H87",
                Descripcion = "Test Product",
                ValorUnitario = l.UnitPrice,
                Importe = importe,
                Descuento = discount,
                ObjetoImp = l.TaxRate > 0 ? "02" : "01",
                Impuestos = l.TaxRate > 0 ? new ConceptoImpuestos
                {
                    Traslados = new List<ConceptoTraslado>
                    {
                        new() { Base = taxBase, Impuesto = "002", TipoFactor = "Tasa",
                                TasaOCuota = l.TaxRate, Importe = taxImporte }
                    }
                } : null
            };
        }).ToList();

        // Document-level totals (2 decimals)
        var cfdiTotalImpuestos = Math.Round(
            conceptos.Where(c => c.Impuestos?.Traslados != null)
                     .SelectMany(c => c.Impuestos!.Traslados!)
                     .Sum(t => t.Importe), 2);
        var cfdiSubTotal = Math.Round(conceptos.Sum(c => c.Importe), 2);
        var cfdiDescuento = Math.Round(conceptos.Sum(c => c.Descuento), 2);
        var cfdiTotal = cfdiSubTotal - cfdiDescuento + cfdiTotalImpuestos;

        // Rounding adjustment (positive only)
        var adjustment = saleTotal - cfdiTotal;
        if (adjustment > 0)
        {
            conceptos.Add(new Concepto
            {
                ClaveProdServ = "84111506",
                Cantidad = 1,
                ClaveUnidad = "ACT",
                Descripcion = "Ajuste por redondeo",
                ValorUnitario = adjustment,
                Importe = adjustment,
                ObjetoImp = "01"
            });

            cfdiSubTotal = Math.Round(conceptos.Sum(c => c.Importe), 2);
            cfdiDescuento = Math.Round(conceptos.Sum(c => c.Descuento), 2);
            cfdiTotal = cfdiSubTotal - cfdiDescuento + cfdiTotalImpuestos;
        }

        return (conceptos, cfdiSubTotal, cfdiDescuento, cfdiTotalImpuestos, cfdiTotal);
    }

    #endregion

    [Test]
    public void ZeroAdjustment_NoRedondeoConcepto()
    {
        // 1 unit × $10.00 = $10.00 + $1.60 IVA = $11.60 — exact, no rounding
        var lines = new[] { (Qty: 1m, UnitPrice: 10.00m, Discount: 0m, TaxRate: 0.16m) };
        var saleTotal = 11.60m;

        var (conceptos, _, _, _, cfdiTotal) = SimulateBuildComprobante(lines, saleTotal);

        Assert.That(conceptos.Count, Is.EqualTo(1), "No rounding concepto should be added");
        Assert.That(cfdiTotal, Is.EqualTo(saleTotal));
    }

    [Test]
    public void PositiveAdjustment_AddsRedondeoConcepto()
    {
        // POS rounding: sale.Total = $12.00 but CFDI arithmetic = $11.60
        var lines = new[] { (Qty: 1m, UnitPrice: 10.00m, Discount: 0m, TaxRate: 0.16m) };
        var saleTotal = 12.00m; // POS Ceiling rounding to integer

        var (conceptos, subTotal, _, _, cfdiTotal) = SimulateBuildComprobante(lines, saleTotal);

        Assert.That(conceptos.Count, Is.EqualTo(2), "Rounding concepto should be added");
        Assert.That(cfdiTotal, Is.EqualTo(saleTotal), "CFDI total must match sale total");

        var roundingConcepto = conceptos.Last();
        Assert.That(roundingConcepto.ClaveProdServ, Is.EqualTo("84111506"));
        Assert.That(roundingConcepto.ClaveUnidad, Is.EqualTo("ACT"));
        Assert.That(roundingConcepto.Importe, Is.EqualTo(0.40m));
        Assert.That(roundingConcepto.ObjetoImp, Is.EqualTo("01"), "Must be non-taxable");
        Assert.That(roundingConcepto.Impuestos, Is.Null, "Must have no tax node");
    }

    [Test]
    public void RedondeoConcepto_IsNonTaxable()
    {
        var lines = new[] { (Qty: 1m, UnitPrice: 50.00m, Discount: 0m, TaxRate: 0.16m) };
        var saleTotal = 59.00m; // Ceiling: 58.00 → 59.00

        var (conceptos, _, _, totalImpuestos, _) = SimulateBuildComprobante(lines, saleTotal);

        // Tax should only come from the product, not from rounding concepto
        Assert.That(totalImpuestos, Is.EqualTo(8.00m), "Tax must not include rounding");
        Assert.That(conceptos.Last().ObjetoImp, Is.EqualTo("01"));
    }

    [Test]
    public void NegativeAdjustment_NoConceptoAdded()
    {
        // Edge case: CFDI total is higher than sale total (no concepto added)
        var lines = new[] { (Qty: 1m, UnitPrice: 10.00m, Discount: 0m, TaxRate: 0.16m) };
        var saleTotal = 11.59m; // Hypothetically less than CFDI $11.60

        var (conceptos, _, _, _, cfdiTotal) = SimulateBuildComprobante(lines, saleTotal);

        Assert.That(conceptos.Count, Is.EqualTo(1), "No concepto for negative adjustment");
        Assert.That(cfdiTotal, Is.EqualTo(11.60m), "CFDI keeps its arithmetic total");
    }

    [Test]
    public void CfdiConsistency_WithRounding_TotalEquation()
    {
        // SubTotal - Descuento + IVA = Total must hold
        var lines = new[]
        {
            (Qty: 3m, UnitPrice: 10.50m, Discount: 0m, TaxRate: 0.16m),
            (Qty: 2m, UnitPrice: 25.00m, Discount: 5.00m, TaxRate: 0.16m),
        };
        // Arithmetic total = 81.50 - 5.00 + 12.24 = 88.74 → Ceiling to 89.00
        var saleTotal = 89.00m;

        var (_, subTotal, descuento, totalImpuestos, cfdiTotal) =
            SimulateBuildComprobante(lines, saleTotal);

        Assert.That(cfdiTotal, Is.EqualTo(subTotal - descuento + totalImpuestos),
            "CFDI40119: Total = SubTotal - Descuento + TotalImpuestosTrasladados");
        Assert.That(cfdiTotal, Is.EqualTo(saleTotal), "Must match sale total");
    }

    [Test]
    public void SixDecimalPrecision_BridgesPOSDifference()
    {
        // The classic scenario: $6.034483 without IVA = $7.00 with IVA
        // POS (6-dec): total = $7.00
        // CFDI (6-dec lines, 2-dec doc): SubTotal=6.03, IVA=0.97, Total=$7.00
        // No rounding concepto needed — 6-dec precision matches POS
        var lines = new[] { (Qty: 1m, UnitPrice: 6.034483m, Discount: 0m, TaxRate: 0.16m) };
        var saleTotal = 7.00m;

        var (conceptos, subTotal, _, totalImpuestos, cfdiTotal) =
            SimulateBuildComprobante(lines, saleTotal);

        Assert.That(conceptos.Count, Is.EqualTo(1), "No rounding needed with 6-dec precision");
        Assert.That(cfdiTotal, Is.EqualTo(7.00m));
        Assert.That(subTotal, Is.EqualTo(6.03m));
        Assert.That(totalImpuestos, Is.EqualTo(0.97m));
    }

    [Test]
    public void WholesaleDiscount_FullPrecision_CfdiMatchesPOS()
    {
        // Regression: Sale #19 — two products at wholesale with fractional discounts.
        // When DiscountAmount was stored at 2 decimals (34.48 instead of 34.482760),
        // the CFDI total was $480.01 instead of $480.00.
        //
        // P0159: 40 × $6.034483, wholesale ~14.29% discount ($5.172414 fixed)
        //   Discount% = (6.034483 - 5.172414) / 6.034483 × 100 = 14.285711...%
        // P0176: 40 × $6.465517, wholesale 20% discount ($5.172414 fixed)

        var discount1Pct = (6.034483m - 5.172414m) / 6.034483m * 100;
        var discount2Pct = 20m;

        var gross1 = Math.Round(40m * 6.034483m, 6); // 241.379320
        var gross2 = Math.Round(40m * 6.465517m, 6); // 258.620680

        // Full-precision discounts (as stored in DB with decimal(10,6))
        var disc1 = Math.Round(gross1 * discount1Pct / 100, 6); // 34.482760
        var disc2 = Math.Round(gross2 * discount2Pct / 100, 6); // 51.724136

        var lines = new[]
        {
            (Qty: 40m, UnitPrice: 6.034483m, Discount: disc1, TaxRate: 0.16m),
            (Qty: 40m, UnitPrice: 6.465517m, Discount: disc2, TaxRate: 0.16m),
        };

        // POS total: $480.00 (40 × $6 + 40 × $6 with IVA-round prices)
        var saleTotal = 480.00m;

        var (conceptos, subTotal, descuento, totalImpuestos, cfdiTotal) =
            SimulateBuildComprobante(lines, saleTotal);

        // Verify CFDI matches POS — no rounding concepto needed
        Assert.That(cfdiTotal, Is.EqualTo(480.00m),
            "CFDI total must be $480.00, not $480.01 (regression: truncated discount bug)");
        Assert.That(conceptos.Count, Is.EqualTo(2),
            "No rounding concepto should be needed when discounts have full precision");
        Assert.That(subTotal, Is.EqualTo(500.00m));

        // Verify line-level discounts use 6 decimals
        Assert.That(conceptos[0].Descuento, Is.EqualTo(disc1).Within(0.000001m),
            "P0159 discount must preserve 6-decimal precision");
        Assert.That(conceptos[1].Descuento, Is.EqualTo(disc2).Within(0.000001m),
            "P0176 discount must preserve 6-decimal precision");

        // Verify CFDI equation holds
        Assert.That(cfdiTotal, Is.EqualTo(subTotal - descuento + totalImpuestos),
            "CFDI40119: Total = SubTotal - Descuento + TotalImpuestosTrasladados");
    }

    [Test]
    public void FormatLineAmount_UsesSixDecimals()
    {
        Assert.That(CfdiFormatHelper.FormatLineAmount(6.034483m), Is.EqualTo("6.034483"));
        Assert.That(CfdiFormatHelper.FormatLineAmount(0.965517m), Is.EqualTo("0.965517"));
        Assert.That(CfdiFormatHelper.FormatLineAmount(100.00m), Is.EqualTo("100.000000"));
    }
}
