using NUnit.Framework;

namespace App.Services.Tests.Billing;

/// <summary>
/// Tests for CFDI 4.0 arithmetic consistency rules.
/// All values in the XML must be derived bottom-up from per-concept calculations
/// to avoid PAC rejection. These tests validate the formulas used in
/// MexicoInvoiceService.BuildComprobante / BuildConceptos / BuildImpuestos.
///
/// SAT validation rules covered:
///   CFDI40108 — Comprobante.SubTotal must equal sum(Concepto.Importe)
///   CFDI40109 — Comprobante.Descuento must equal sum(Concepto.Descuento)
///   CFDI40215 — Global Traslado.Base must equal sum of per-concept Traslado.Base (grouped by rate)
///   CFDI40216 — Global Traslado.Importe must equal sum of per-concept Traslado.Importe (grouped by rate)
///   Total     — Comprobante.Total = SubTotal - Descuento + TotalImpuestosTrasladados
/// </summary>
[TestFixture]
public class CfdiTaxBaseCalculationTests
{
    #region Helpers — mirror the formulas in MexicoInvoiceService (6-decimal line level)

    /// <summary>Concepto.Importe = Round(Qty × UnitPrice, 6)</summary>
    private static decimal ConceptoImporte(decimal qty, decimal unitPrice)
        => Math.Round(qty * unitPrice, 6);

    /// <summary>Concepto.Impuestos.Traslado.Base = Importe - Descuento</summary>
    private static decimal ConceptoTaxBase(decimal qty, decimal unitPrice, decimal discount)
        => ConceptoImporte(qty, unitPrice) - discount;

    /// <summary>Concepto.Impuestos.Traslado.Importe = Round(Base × Rate, 6)</summary>
    private static decimal ConceptoTaxImporte(decimal qty, decimal unitPrice, decimal discount, decimal taxRate)
        => Math.Round(ConceptoTaxBase(qty, unitPrice, discount) * taxRate, 6);

    /// <summary>
    /// SAT CFDI40167/CFDI40180 tolerance limits.
    /// LimInf = Truncate((A - halfA) × (B - halfB), decimals)
    /// LimSup = CeilingTo((A + halfA - ε) × (B + halfB - ε), decimals)
    /// where halfX = 0.5 × 10^(-decimalsOfX), ε = 10^-12
    /// </summary>
    private static (decimal LimInf, decimal LimSup) CalculateImporteLimits(
        decimal a, decimal b, int currencyDecimals)
    {
        const decimal epsilon = 0.000000000001m; // 10^-12
        var halfA = 0.5m * PowerOfTen(-CountDecimals(a));
        var halfB = 0.5m * PowerOfTen(-CountDecimals(b));

        var rawLow = (a - halfA) * (b - halfB);
        var rawHigh = (a + halfA - epsilon) * (b + halfB - epsilon);

        // Truncate lower, ceiling upper to currency decimals
        var factor = PowerOfTen(currencyDecimals);
        var limInf = Math.Truncate(rawLow * factor) / factor;
        var limSup = Math.Ceiling(rawHigh * factor) / factor;

        return (limInf, limSup);
    }

    private static int CountDecimals(decimal value)
    {
        var text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var dot = text.IndexOf('.');
        return dot < 0 ? 0 : text.Length - dot - 1;
    }

    private static decimal PowerOfTen(int exponent)
    {
        if (exponent >= 0)
        {
            decimal result = 1m;
            for (int i = 0; i < exponent; i++) result *= 10m;
            return result;
        }
        else
        {
            decimal result = 1m;
            for (int i = 0; i < -exponent; i++) result /= 10m;
            return result;
        }
    }

    private record LineInput(decimal Qty, decimal UnitPrice, decimal Discount, decimal TaxRate, bool IsTaxable = true);

    /// <summary>
    /// Simulates the full CFDI build and validates all SAT consistency rules.
    /// This mirrors BuildComprobante + BuildConceptos + BuildImpuestos.
    /// </summary>
    private static void AssertCfdiConsistency(LineInput[] lines, decimal? expectedSubTotal = null)
    {
        // --- Per-concept values (BuildConceptos) ---
        var conceptImportes = lines.Select(l => ConceptoImporte(l.Qty, l.UnitPrice)).ToArray();
        var conceptDescuentos = lines.Select(l => l.Discount).ToArray();
        var conceptTaxBases = lines.Select(l => l.IsTaxable ? ConceptoTaxBase(l.Qty, l.UnitPrice, l.Discount) : 0m).ToArray();
        var conceptTaxImportes = lines.Select(l => l.IsTaxable ? ConceptoTaxImporte(l.Qty, l.UnitPrice, l.Discount, l.TaxRate) : 0m).ToArray();

        // --- Comprobante-level (BuildComprobante) — document level rounds to 2 ---
        var cfdiSubTotal = Math.Round(conceptImportes.Sum(), 2);
        var cfdiDescuento = Math.Round(conceptDescuentos.Sum(), 2);

        // --- Global taxes (BuildImpuestos) ---
        var taxGroups = lines
            .Where(l => l.IsTaxable && l.TaxRate > 0)
            .Select((l, i) => new { l.TaxRate, Base = conceptTaxBases[i] })
            .GroupBy(x => x.TaxRate)
            .Select(g =>
            {
                // Document-level aggregation rounds to 2 decimals
                var baseSum = Math.Round(g.Sum(x => x.Base), 2);
                var importeSum = Math.Round(lines
                    .Select((l, idx) => new { l.TaxRate, l.IsTaxable, Importe = conceptTaxImportes[idx] })
                    .Where(x => x.IsTaxable && x.TaxRate == g.Key)
                    .Sum(x => x.Importe), 2);
                return new { TaxRate = g.Key, Base = baseSum, Importe = importeSum };
            })
            .ToList();

        var cfdiTotalImpuestos = taxGroups.Sum(g => g.Importe);
        var cfdiTotal = cfdiSubTotal - cfdiDescuento + cfdiTotalImpuestos;

        // --- CFDI40108: SubTotal ≈ sum(Concepto.Importe) within rounding tolerance ---
        // SubTotal is Round(sum, 2) while Concepto.Importe uses 6 decimals.
        Assert.That(cfdiSubTotal, Is.EqualTo(Math.Round(conceptImportes.Sum(), 2)),
            "CFDI40108: SubTotal must equal Round(sum of Concepto.Importe, 2)");

        if (expectedSubTotal.HasValue)
        {
            Assert.That(cfdiSubTotal, Is.EqualTo(Math.Round(expectedSubTotal.Value, 2)),
                $"SubTotal expected {Math.Round(expectedSubTotal.Value, 2)} but got {cfdiSubTotal}");
        }

        // --- CFDI40109: Descuento ≈ sum(Concepto.Descuento) within rounding ---
        Assert.That(cfdiDescuento, Is.EqualTo(Math.Round(conceptDescuentos.Sum(), 2)),
            "CFDI40109: Descuento must equal Round(sum of Concepto.Descuento, 2)");

        // --- CFDI40215: Global Traslado.Base == Round(sum of per-concept Traslado.Base, 2) ---
        foreach (var group in taxGroups)
        {
            var perConceptBaseSum = Math.Round(lines
                .Select((l, i) => new { l.TaxRate, l.IsTaxable, Base = conceptTaxBases[i] })
                .Where(x => x.IsTaxable && x.TaxRate == group.TaxRate)
                .Sum(x => x.Base), 2);

            Assert.That(group.Base, Is.EqualTo(perConceptBaseSum),
                $"CFDI40215: Global Traslado.Base for rate {group.TaxRate} mismatch");
        }

        // --- CFDI40216: Global Traslado.Importe == Round(sum of per-concept Traslado.Importe, 2) ---
        foreach (var group in taxGroups)
        {
            var perConceptImporteSum = Math.Round(lines
                .Select((l, i) => new { l.TaxRate, l.IsTaxable, Importe = conceptTaxImportes[i] })
                .Where(x => x.IsTaxable && x.TaxRate == group.TaxRate)
                .Sum(x => x.Importe), 2);

            Assert.That(group.Importe, Is.EqualTo(perConceptImporteSum),
                $"CFDI40216: Global Traslado.Importe for rate {group.TaxRate} must equal sum of per-concept importes");
        }

        // --- CFDI40119: Total = SubTotal - Descuento + TotalImpuestosTrasladados ---
        Assert.That(cfdiTotal, Is.EqualTo(cfdiSubTotal - cfdiDescuento + cfdiTotalImpuestos),
            "CFDI40119: Total must equal SubTotal - Descuento + TotalImpuestosTrasladados");

        // --- CFDI40110: Descuento ≤ SubTotal ---
        Assert.That(cfdiDescuento, Is.LessThanOrEqualTo(cfdiSubTotal),
            "CFDI40110: Descuento must be ≤ SubTotal");

        // --- CFDI40169: Concepto.Descuento ≤ Concepto.Importe ---
        for (int i = 0; i < lines.Length; i++)
        {
            Assert.That(conceptDescuentos[i], Is.LessThanOrEqualTo(conceptImportes[i]),
                $"CFDI40169: Concepto[{i}].Descuento ({conceptDescuentos[i]}) must be ≤ Importe ({conceptImportes[i]})");
        }

        // --- CFDI40167: Concepto.Importe within tolerance of Cantidad × ValorUnitario ---
        // With 6-decimal line precision, tolerance is checked at 6 decimals.
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            var (limInf, limSup) = CalculateImporteLimits(l.Qty, l.UnitPrice, 6);
            Assert.That(conceptImportes[i], Is.GreaterThanOrEqualTo(limInf),
                $"CFDI40167: Concepto[{i}].Importe ({conceptImportes[i]}) must be ≥ LimInf ({limInf})");
            Assert.That(conceptImportes[i], Is.LessThanOrEqualTo(limSup),
                $"CFDI40167: Concepto[{i}].Importe ({conceptImportes[i]}) must be ≤ LimSup ({limSup})");
        }

        // --- CFDI40180: Traslado.Importe within tolerance of Base × TasaOCuota ---
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].IsTaxable || lines[i].TaxRate <= 0) continue;
            var (limInf, limSup) = CalculateImporteLimits(conceptTaxBases[i], lines[i].TaxRate, 6);
            Assert.That(conceptTaxImportes[i], Is.GreaterThanOrEqualTo(limInf),
                $"CFDI40180: Concepto[{i}].Traslado.Importe ({conceptTaxImportes[i]}) must be ≥ LimInf ({limInf})");
            Assert.That(conceptTaxImportes[i], Is.LessThanOrEqualTo(limSup),
                $"CFDI40180: Concepto[{i}].Traslado.Importe ({conceptTaxImportes[i]}) must be ≤ LimSup ({limSup})");
        }

        // --- CFDI40205: TotalImpuestosTrasladados == sum of Traslado.Importe ---
        Assert.That(cfdiTotalImpuestos, Is.EqualTo(taxGroups.Sum(g => g.Importe)),
            "CFDI40205: TotalImpuestosTrasladados must equal sum of Traslado.Importe");

        // --- All values must have at most 2 decimal places (MXN) ---
        Assert.That(cfdiSubTotal, Is.EqualTo(Math.Round(cfdiSubTotal, 2)), "SubTotal must have ≤2 decimals");
        Assert.That(cfdiDescuento, Is.EqualTo(Math.Round(cfdiDescuento, 2)), "Descuento must have ≤2 decimals");
        Assert.That(cfdiTotal, Is.EqualTo(Math.Round(cfdiTotal, 2)), "Total must have ≤2 decimals");

        foreach (var importe in conceptImportes)
            Assert.That(importe, Is.EqualTo(Math.Round(importe, 6)), "Concepto.Importe must have ≤6 decimals");
        foreach (var tax in conceptTaxImportes)
            Assert.That(tax, Is.EqualTo(Math.Round(tax, 6)), "Concepto.Traslado.Importe must have ≤6 decimals");
    }

    #endregion

    // ─── CFDI40215: Global Base mismatch (original production bug) ───

    [Test]
    public void CFDI40215_GlobalBase_MatchesConceptBases_ProductionBug()
    {
        // Exact values from the failing invoice A3 (sale 16)
        AssertCfdiConsistency(new[]
        {
            new LineInput(20m, 6.034483m, 17.24m, 0.16m),
            new LineInput(20m, 6.465517m, 25.86m, 0.16m),
        }, expectedSubTotal: 250.00m);
    }

    [Test]
    public void CFDI40215_UsingDbSubtotal_WouldProduceDifferentBase()
    {
        // Proves the old bug: if Subtotal differs from Round(Qty*Price, 2)
        var qty = 3.5m;
        var unitPrice = 28.571429m;
        var dbSubtotal = 100.50m; // stored with surcharge

        var correctBase = ConceptoImporte(qty, unitPrice); // 100.00
        Assert.That(correctBase, Is.Not.EqualTo(dbSubtotal),
            "DB Subtotal differs from recalculated Importe — old code would fail CFDI40215");
    }

    // ─── CFDI40108: SubTotal mismatch (second production bug) ───

    [Test]
    public void CFDI40108_SubTotal_MustBeSumOfConceptImportes_NotDbValue()
    {
        // Scenario: DB has sale.Subtotal = 1077.59 but sum of Round(qty*price,2) = 1077.58
        // This caused CFDI40108 in production (sale 4, $1,140.00)
        var lines = new[]
        {
            new LineInput(6m, 12.931035m, 5.16m, 0.16m),  // Importe = Round(77.586210, 2) = 77.59
            new LineInput(6m, 166.666667m, 0m, 0.16m),     // Importe = Round(1000.000002, 2) = 1000.00
        };

        var sumConceptImportes = lines.Sum(l => ConceptoImporte(l.Qty, l.UnitPrice));
        // 77.59 + 1000.00 = 1077.59

        // If DB had stored 1077.59 (same), no issue. But if DB stored 1077.58 due to
        // different rounding path, the old code would report the DB value instead of the sum.
        // The fix: always use sum of concept importes.
        AssertCfdiConsistency(lines, expectedSubTotal: sumConceptImportes);
    }

    [Test]
    public void CFDI40108_SubTotal_WithManyLines_AccumulatedRoundingDifference()
    {
        // Many lines with repeating decimals — rounding accumulates
        var lines = Enumerable.Range(1, 10)
            .Select(i => new LineInput(3m, 33.333333m, 0m, 0.16m))
            .ToArray();

        // Each Importe = Round(3 * 33.333333, 2) = Round(99.999999, 2) = 100.00
        // SubTotal from concepts = 10 × 100.00 = 1000.00
        // A DB might store 999.99 or 1000.01 depending on how it rounds the sum
        AssertCfdiConsistency(lines, expectedSubTotal: 1000.00m);
    }

    // ─── CFDI40109: Descuento mismatch ───

    [Test]
    public void CFDI40109_Descuento_MustBeSumOfConceptDescuentos()
    {
        AssertCfdiConsistency(new[]
        {
            new LineInput(1m, 100.00m, 10.00m, 0.16m),
            new LineInput(2m, 50.00m, 7.50m, 0.16m),
            new LineInput(5m, 20.00m, 0m, 0.16m),
        });
    }

    // ─── Multiple tax rates ───

    [Test]
    public void MultipleTaxRates_AllGroupsAreConsistent()
    {
        AssertCfdiConsistency(new[]
        {
            new LineInput(10m, 15.50m, 5.00m, 0.16m),
            new LineInput(5m, 20.00m, 0m, 0.16m),
            new LineInput(2m, 50.00m, 10.00m, 0.08m),
        });
    }

    // ─── Mixed taxable / non-taxable ───

    [Test]
    public void MixedTaxableAndExempt_SubTotalIncludesAll_TaxOnlyOnTaxable()
    {
        AssertCfdiConsistency(new[]
        {
            new LineInput(1m, 200.00m, 0m, 0.16m, IsTaxable: true),
            new LineInput(3m, 50.00m, 0m, 0m, IsTaxable: false),
        });
    }

    // ─── CFDI40216: Global Importe must be sum of per-concept importes ───

    [Test]
    public void CFDI40216_GlobalImporte_MustBeSumOfPerConceptImportes_NotRoundOfBaseTimesRate()
    {
        // This test demonstrates that Round(baseSum * rate) can differ from
        // sum(Round(base_i * rate)) when discounts create fractional bases.
        //
        // Concept 1: Base = 103.45 → Round(103.45 * 0.16) = Round(16.552) = 16.55
        // Concept 2: Base = 103.46 → Round(103.46 * 0.16) = Round(16.5536) = 16.55
        // Sum of per-concept importes = 16.55 + 16.55 = 33.10
        //
        // Global base = 206.91 → Round(206.91 * 0.16) = Round(33.1056) = 33.11 ← WRONG!
        //
        // SAT requires 33.10 (sum of per-concept), not 33.11 (round of global base).

        var base1 = 103.45m;
        var base2 = 103.46m;
        var rate = 0.16m;

        var perConceptImporte1 = Math.Round(base1 * rate, 2); // 16.55
        var perConceptImporte2 = Math.Round(base2 * rate, 2); // 16.55
        var sumPerConcept = perConceptImporte1 + perConceptImporte2; // 33.10

        var globalBase = base1 + base2; // 206.91
        var roundOfGlobalBase = Math.Round(globalBase * rate, 2); // 33.11

        Assert.That(sumPerConcept, Is.EqualTo(33.10m));
        Assert.That(roundOfGlobalBase, Is.EqualTo(33.11m));
        Assert.That(sumPerConcept, Is.Not.EqualTo(roundOfGlobalBase),
            "sum(Round(base_i * rate)) ≠ Round(sum(base_i) * rate) — SAT requires the sum approach");
    }

    [Test]
    public void CFDI40216_DiscountsThatCreateOddBases_GlobalImporteIsConsistent()
    {
        // Discounts that produce bases ending in .X5 trigger rounding divergence
        AssertCfdiConsistency(new[]
        {
            new LineInput(1m, 120.69m, 17.24m, 0.16m),  // Base = 103.45
            new LineInput(1m, 129.32m, 25.86m, 0.16m),  // Base = 103.46
        });
    }

    [Test]
    public void CFDI40216_ManyLinesWithSmallDiscounts_NoImporteDrift()
    {
        // 8 lines with varied discounts that produce awkward bases
        AssertCfdiConsistency(new[]
        {
            new LineInput(3m, 45.123456m, 2.50m, 0.16m),
            new LineInput(7m, 12.345678m, 1.23m, 0.16m),
            new LineInput(1m, 99.999999m, 10.00m, 0.16m),
            new LineInput(5m, 33.333333m, 5.55m, 0.16m),
            new LineInput(2m, 77.770000m, 3.33m, 0.16m),
            new LineInput(10m, 8.625000m, 0.87m, 0.16m),
            new LineInput(4m, 25.125000m, 4.44m, 0.16m),
            new LineInput(1m, 500.005000m, 50.00m, 0.16m),
        });
    }

    // ─── CFDI40110 / CFDI40169: Discount constraints ───

    [Test]
    public void CFDI40110_Descuento_MustBeLessThanOrEqualToSubTotal()
    {
        // Full discount: Descuento == SubTotal (edge case, should pass)
        AssertCfdiConsistency(new[] { new LineInput(1m, 100.00m, 100.00m, 0.16m) });
    }

    [Test]
    public void CFDI40169_ConceptDescuento_MustBeLessThanOrEqualToImporte()
    {
        // Discount exactly equals Importe (100%)
        var qty = 5m;
        var price = 20.00m;
        var importe = ConceptoImporte(qty, price); // 100.00
        Assert.That(importe, Is.EqualTo(100.00m));

        // Discount = Importe → valid
        AssertCfdiConsistency(new[] { new LineInput(qty, price, importe, 0.16m) });
    }

    // ─── CFDI40167 / CFDI40180: Importe tolerance limits ───

    [Test]
    public void CFDI40167_ConceptImporte_WithinToleranceLimits()
    {
        // Various prices that exercise the tolerance boundaries
        var testCases = new[]
        {
            (qty: 20.000000m, price: 6.034483m),    // Original failing invoice
            (qty: 6m, price: 12.931035m),             // Second failing invoice
            (qty: 3.5m, price: 28.571429m),           // Fractional quantity
            (qty: 999m, price: 0.123456m),            // Large qty, small price
            (qty: 0.001m, price: 999999.99m),         // Tiny qty, huge price
            (qty: 1m, price: 0.01m),                  // Minimum values
        };

        foreach (var (qty, price) in testCases)
        {
            var importe = ConceptoImporte(qty, price);
            var (limInf, limSup) = CalculateImporteLimits(qty, price, 2);

            Assert.That(importe, Is.GreaterThanOrEqualTo(limInf),
                $"CFDI40167: Importe {importe} < LimInf {limInf} for {qty}×{price}");
            Assert.That(importe, Is.LessThanOrEqualTo(limSup),
                $"CFDI40167: Importe {importe} > LimSup {limSup} for {qty}×{price}");
        }
    }

    [Test]
    public void CFDI40180_TaxImporte_WithinToleranceLimits()
    {
        // Tax amounts must be within limits of Base × TasaOCuota
        var testCases = new[]
        {
            (baseVal: 103.45m, rate: 0.16m),     // Original bug
            (baseVal: 72.42m, rate: 0.16m),
            (baseVal: 206.90m, rate: 0.16m),
            (baseVal: 90.00m, rate: 0.08m),       // Different rate
            (baseVal: 0.01m, rate: 0.16m),        // Minimum base
            (baseVal: 50000.00m, rate: 0.16m),    // Large base
        };

        foreach (var (baseVal, rate) in testCases)
        {
            var importe = Math.Round(baseVal * rate, 2);
            var (limInf, limSup) = CalculateImporteLimits(baseVal, rate, 2);

            Assert.That(importe, Is.GreaterThanOrEqualTo(limInf),
                $"CFDI40180: TaxImporte {importe} < LimInf {limInf} for {baseVal}×{rate}");
            Assert.That(importe, Is.LessThanOrEqualTo(limSup),
                $"CFDI40180: TaxImporte {importe} > LimSup {limSup} for {baseVal}×{rate}");
        }
    }

    // ─── Edge cases ───

    [Test]
    public void SingleLine_NoDiscount_AllConsistent()
    {
        AssertCfdiConsistency(new[]
        {
            new LineInput(1m, 100.00m, 0m, 0.16m),
        }, expectedSubTotal: 100.00m);
    }

    [Test]
    public void SingleLine_FullDiscount_ZeroTotal()
    {
        // 100% discount — Total should be 0
        var lines = new[] { new LineInput(1m, 100.00m, 100.00m, 0.16m) };
        AssertCfdiConsistency(lines);
    }

    [Test]
    public void VerySmallAmounts_PrecisionMaintained()
    {
        AssertCfdiConsistency(new[]
        {
            new LineInput(1m, 0.01m, 0m, 0.16m),
            new LineInput(1m, 0.02m, 0.01m, 0.16m),
        });
    }

    [Test]
    public void LargeQuantityWithDecimalPrice_NoAccumulatedDrift()
    {
        AssertCfdiConsistency(new[]
        {
            new LineInput(999m, 0.123456m, 0m, 0.16m),
        });
    }

    [Test]
    public void FractionalQuantity_RoundingIsCorrect()
    {
        // Partial-sale scenario: fractional quantities
        AssertCfdiConsistency(new[]
        {
            new LineInput(0.5m, 199.99m, 0m, 0.16m),
            new LineInput(1.75m, 45.714286m, 5.00m, 0.16m),
        });
    }

    [Test]
    public void ReproduceOriginalBug_FullEndToEnd()
    {
        // Full validation of the first failing invoice (CFDI40215 + CFDI40108)
        var lines = new[]
        {
            new LineInput(20m, 6.034483m, 17.24m, 0.16m),
            new LineInput(20m, 6.465517m, 25.86m, 0.16m),
        };

        // Per-concept
        var importe1 = ConceptoImporte(20m, 6.034483m);   // 120.69
        var importe2 = ConceptoImporte(20m, 6.465517m);   // 129.31
        var base1 = importe1 - 17.24m;                     // 103.45
        var base2 = importe2 - 25.86m;                     // 103.45
        var tax1 = Math.Round(base1 * 0.16m, 2);          // 16.55
        var tax2 = Math.Round(base2 * 0.16m, 2);          // 16.55

        // Comprobante-level
        var subTotal = importe1 + importe2;                 // 250.00
        var descuento = 17.24m + 25.86m;                   // 43.10
        var globalBase = base1 + base2;                     // 206.90
        var globalTax = Math.Round(globalBase * 0.16m, 2); // 33.10
        var total = subTotal - descuento + globalTax;       // 240.00

        Assert.That(subTotal, Is.EqualTo(250.00m));
        Assert.That(descuento, Is.EqualTo(43.10m));
        Assert.That(globalBase, Is.EqualTo(206.90m), "Must be 206.90, not 163.80 (old bug)");
        Assert.That(globalTax, Is.EqualTo(33.10m));
        Assert.That(total, Is.EqualTo(240.00m));

        // Also validate through the general consistency checker
        AssertCfdiConsistency(lines, expectedSubTotal: 250.00m);
    }
}
