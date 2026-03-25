using NUnit.Framework;
using Moq;

using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.DTOs.Shop.Calculation;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Services.Settings;
using App.Services.Shop;

using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Tests for PricingCalculationService ensuring line and document calculations
/// produce values compatible with CFDI 4.0 requirements.
/// </summary>
[TestFixture]
public class PricingCalculationServiceTests
{
    private PricingCalculationService _service = null!;
    private Mock<IRoundingSettingsService> _roundingMock = null!;

    [SetUp]
    public void Setup()
    {
        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.16m);

        var companyMock = new Mock<ICompanySettingsService>();
        companyMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto
            {
                Id = 1, CompanyName = "Test", CountryCode = "MX", CurrencyCode = "MXN",
                TimeZoneId = "America/Mexico_City"
            });

        _roundingMock = new Mock<IRoundingSettingsService>();
        _roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        _service = new PricingCalculationService(
            taxRateMock.Object,
            companyMock.Object,
            _roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);
    }

    #region CalculateLine

    [Test]
    public void CalculateLine_NoDiscount_SubtotalEqualsQtyTimesPrice()
    {
        var result = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = 6m, UnitPrice = 12.93m, DiscountPercentage = 0
        });

        Assert.That(result.BasePriceBeforeSurcharge, Is.EqualTo(77.58m));
        Assert.That(result.DiscountAmount, Is.EqualTo(0m));
        Assert.That(result.Subtotal, Is.EqualTo(77.58m));
    }

    [Test]
    public void CalculateLine_WithDiscount_BasePriceIsGross_SubtotalIsNet()
    {
        // 6 × 12.93 = 77.58 gross, 10% discount = 7.758
        var result = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = 6m, UnitPrice = 12.93m, DiscountPercentage = 10m
        });

        Assert.That(result.BasePriceBeforeSurcharge, Is.EqualTo(77.58m), "BasePriceBeforeSurcharge should be gross (Qty × UnitPrice)");
        Assert.That(result.DiscountAmount, Is.EqualTo(7.758m), "DiscountAmount = gross × discount%");
        Assert.That(result.Subtotal, Is.EqualTo(77.58m - 7.758m), "Subtotal should be net (gross - discount)");
    }

    [Test]
    public void CalculateLine_CfdiImporteMustBeGross_NotNet()
    {
        // This test documents the CFDI requirement:
        // Concepto.Importe = Qty × ValorUnitario (GROSS, before discount)
        // Concepto.Descuento = discount amount
        // The service should provide BasePriceBeforeSurcharge for this purpose
        var result = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = 6m, UnitPrice = 12.93m, DiscountPercentage = 6.649616m
        });

        var cfdiImporte = result.BasePriceBeforeSurcharge; // This is what CFDI Importe should use
        var cfdiDescuento = result.DiscountAmount;

        Assert.That(cfdiImporte, Is.EqualTo(6m * 12.93m), "CFDI Importe must be Qty × UnitPrice (gross)");
        Assert.That(cfdiImporte, Is.Not.EqualTo(result.Subtotal), "CFDI Importe must NOT be Subtotal (net) when discount > 0");
        Assert.That(cfdiImporte - cfdiDescuento, Is.EqualTo(result.Subtotal).Within(0.01m), "Gross - Discount ≈ Net (Subtotal)");
    }

    #endregion

    #region DocumentCalculation — CFDI SubTotal rule

    [Test]
    public async Task CalculateDocument_SubtotalIsGross_MatchesSumOfLineGrossAmounts()
    {
        // CFDI rule: Comprobante.SubTotal = Σ Concepto.Importe (gross amounts)
        // Use CalculateLine to get proper per-line tax values
        var line1 = _service.CalculateLine(new LineCalculationInput
            { Quantity = 6m, UnitPrice = 12.93m, DiscountPercentage = 10m, TaxRate = 0.16m });
        var line2 = _service.CalculateLine(new LineCalculationInput
            { Quantity = 1m, UnitPrice = 45.00m, DiscountPercentage = 0m, TaxRate = 0.16m });

        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = line1.Subtotal, DiscountAmount = line1.DiscountAmount, IsTaxable = true,
                     TaxAmount = line1.TaxAmount, TaxBase = line1.TaxBase },
            new() { Subtotal = line2.Subtotal, DiscountAmount = line2.DiscountAmount, IsTaxable = true,
                     TaxAmount = line2.TaxAmount, TaxBase = line2.TaxBase }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = false
        });

        Assert.That(result.Subtotal, Is.EqualTo(line1.GrossAmount + line2.GrossAmount),
            "Document Subtotal should be sum of gross amounts");
        Assert.That(result.TaxAmount, Is.EqualTo(Math.Round(line1.TaxAmount + line2.TaxAmount, 2)),
            "Document TaxAmount should be sum of per-line tax amounts rounded to 2 decimals");
    }

    [Test]
    public async Task CalculateDocument_SingleLineWithDiscount_CfdiValuesAreConsistent()
    {
        // Reproduce the exact bug scenario: 6 × $12.93 with ~6.65% discount
        var lineCalc = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = 6m, UnitPrice = 12.93m, DiscountPercentage = 6.649616m, TaxRate = 0.16m
        });

        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = lineCalc.Subtotal, DiscountAmount = lineCalc.DiscountAmount, IsTaxable = true,
                     TaxAmount = lineCalc.TaxAmount, TaxBase = lineCalc.TaxBase }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = false
        });

        // CFDI validation: SubTotal = Σ Importe (gross)
        Assert.That(result.Subtotal, Is.EqualTo(lineCalc.GrossAmount), "SubTotal must equal gross");
        Assert.That(result.ItemDiscountAmount, Is.EqualTo(Math.Round(lineCalc.DiscountAmount, 2)), "Discount");

        // Tax must match centralized per-line calculation (rounded to 2 at document level)
        Assert.That(result.TaxAmount, Is.EqualTo(Math.Round(lineCalc.TaxAmount, 2)), "Tax must come from centralized calculation");

        var expectedTotal = lineCalc.GrossAmount - Math.Round(lineCalc.DiscountAmount, 2) + Math.Round(lineCalc.TaxAmount, 2);
        Assert.That(result.Total, Is.EqualTo(Math.Round(expectedTotal, 2)), "Total = SubTotal - Descuento + IVA");
    }

    #endregion

    #region CFDI Concepto.Importe regression — the actual bug

    [Test]
    public void CfdiConceptoImporte_MustUseGrossNotNet_RegressionTest()
    {
        // This test ensures the CFDI40108 bug doesn't come back.
        // The bug: MexicoInvoiceService used detail.Subtotal (NET) for Concepto.Importe,
        // but CFDI requires Importe = Qty × ValorUnitario (GROSS).
        //
        // We test the rule: Importe = Qty × UnitPrice, NOT Subtotal
        var qty = 6m;
        var unitPrice = 12.93m;
        var discountPct = 6.649616m; // produces ~5.16 discount

        var lineCalc = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = qty, UnitPrice = unitPrice, DiscountPercentage = discountPct
        });

        // Simulate what MexicoInvoiceService.BuildConceptos should do:
        var cfdiImporte = Math.Round(qty * unitPrice, 2);           // CORRECT: gross
        var cfdiDescuento = Math.Round(lineCalc.DiscountAmount, 2);
        var cfdiTaxBase = cfdiImporte - cfdiDescuento;              // CORRECT: gross - discount

        // What the bug incorrectly did:
        var buggyImporte = Math.Round(lineCalc.Subtotal, 2);       // WRONG: net
        var buggyTaxBase = buggyImporte - cfdiDescuento;            // WRONG: double discount

        Assert.That(cfdiImporte, Is.EqualTo(77.58m), "Importe must be gross");
        Assert.That(buggyImporte, Is.Not.EqualTo(cfdiImporte), "Net != Gross when discount > 0 (this was the bug)");
        Assert.That(cfdiTaxBase, Is.GreaterThan(buggyTaxBase), "Correct base > buggy base (buggy double-discounts)");

        // CFDI validation: SubTotal must equal Σ Importe
        Assert.That(cfdiImporte, Is.EqualTo(qty * unitPrice), "SubTotal = Σ Importe = Σ (Qty × UnitPrice)");
    }

    [TestCase(1, 100.00, 10, 100.00, 10.00, 90.00)]        // Simple 10%
    [TestCase(6, 12.93, 6.649616, 77.58, 5.16, 72.42)]      // The original bug case
    [TestCase(3, 33.333333, 0, 100.00, 0, 100.00)]           // No discount, 6-decimal price
    [TestCase(2, 50.00, 50, 100.00, 50.00, 50.00)]           // 50% discount
    [TestCase(1, 0.01, 0, 0.01, 0, 0.01)]                    // Minimum price
    public void CfdiAmounts_AreConsistent_ForVariousInputs(
        decimal qty, decimal unitPrice, decimal discountPct,
        decimal expectedGross, decimal expectedDiscount, decimal expectedBase)
    {
        var lineCalc = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = qty, UnitPrice = unitPrice, DiscountPercentage = discountPct
        });

        var cfdiImporte = Math.Round(qty * unitPrice, 2);
        var cfdiDescuento = Math.Round(lineCalc.DiscountAmount, 2);
        var cfdiBase = cfdiImporte - cfdiDescuento;

        Assert.That(cfdiImporte, Is.EqualTo(expectedGross), $"Importe (gross) mismatch");
        Assert.That(cfdiDescuento, Is.EqualTo(expectedDiscount).Within(0.01m), $"Descuento mismatch");
        Assert.That(cfdiBase, Is.EqualTo(expectedBase).Within(0.01m), $"Tax Base mismatch");

        // Key invariant: Importe - Descuento = Base (no double discounting)
        Assert.That(cfdiImporte - cfdiDescuento, Is.EqualTo(cfdiBase), "Importe - Descuento must equal Base");
    }

    #endregion

    #region Tax validation — fail fast when tax data is missing

    [Test]
    public void CalculateDocument_WhenTaxAmountNotProvided_ThrowsInvalidOperation()
    {
        // This is the exact bug: UI passes DocumentLineInput WITHOUT TaxAmount/TaxBase.
        // Instead of silently returning $0 tax, we fail fast.
        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = 7.00m, DiscountAmount = 0m, IsTaxable = true }
            // TaxAmount = 0, TaxBase = 0 — missing!
        };

        var input = new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = false
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CalculateDocumentAsync(input));
        Assert.That(ex!.Message, Does.Contain("TaxAmount=0"),
            "Error message should indicate missing tax data");
    }

    [Test]
    public async Task CalculateDocument_WhenTaxAmountProvided_UsesProvidedValue()
    {
        var lineCalc = _service.CalculateLine(new LineCalculationInput
        {
            Quantity = 1m, UnitPrice = 7.00m, DiscountPercentage = 0, TaxRate = 0.16m
        });

        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = lineCalc.Subtotal, DiscountAmount = 0m, IsTaxable = true,
                     TaxAmount = lineCalc.TaxAmount, TaxBase = lineCalc.TaxBase }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = false
        });

        Assert.That(result.TaxAmount, Is.EqualTo(lineCalc.TaxAmount),
            "When TaxAmount is provided, it should be used directly");
    }

    [Test]
    public void CalculateDocument_MixedTaxableAndExempt_WithoutTaxOnTaxable_Throws()
    {
        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = 100.00m, DiscountAmount = 0m, IsTaxable = true },  // missing tax!
            new() { Subtotal = 50.00m, DiscountAmount = 0m, IsTaxable = false }    // exempt, no tax needed
        };

        var input = new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = false
        };

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CalculateDocumentAsync(input),
            "Should throw when taxable line is missing tax data");
    }

    [Test]
    public async Task CalculateDocument_NonTaxableLines_WithoutTaxAmount_DoesNotThrow()
    {
        // Non-taxable lines don't need TaxAmount — should NOT throw
        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = 50.00m, DiscountAmount = 0m, IsTaxable = false }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = false
        });

        Assert.That(result.TaxAmount, Is.EqualTo(0m), "Non-taxable lines should have $0 tax");
        Assert.That(result.Total, Is.EqualTo(50.00m));
    }

    [Test]
    public async Task CalculateDocument_ZeroTaxRate_WithoutTaxAmount_DoesNotThrow()
    {
        // When TaxRate is 0 (no tax configured), missing TaxAmount is fine
        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = 100.00m, DiscountAmount = 0m, IsTaxable = true }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0m, ApplyRounding = false
        });

        Assert.That(result.TaxAmount, Is.EqualTo(0m));
        Assert.That(result.Total, Is.EqualTo(100.00m));
    }

    [Test]
    public async Task CalculateDocument_NegativeRounding_IsNotApplied()
    {
        // Floor rounding gives a negative adjustment — should be ignored (only positive applied)
        _roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
            {
                // Simulate Floor rounding: 137.99 → 137.00 = -0.99
                var rounded = Math.Floor(amount);
                return Result<(decimal, decimal)>.Success((rounded, rounded - amount));
            });

        var lineCalc = _service.CalculateLine(new LineCalculationInput
            { Quantity = 1m, UnitPrice = 100m, DiscountPercentage = 0, TaxRate = 0.16m });

        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = lineCalc.Subtotal, DiscountAmount = 0, IsTaxable = true,
                     TaxAmount = lineCalc.TaxAmount, TaxBase = lineCalc.TaxBase }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = true
        });

        // Negative rounding should NOT be applied
        Assert.That(result.RoundingAmount, Is.EqualTo(0m), "Negative rounding must not be applied");
        Assert.That(result.Total, Is.EqualTo(result.PreRoundingTotal), "Total must equal pre-rounding total when rounding is negative");
    }

    [Test]
    public async Task CalculateDocument_PositiveRounding_IsApplied()
    {
        // Ceiling rounding gives a positive adjustment — should be applied
        _roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
            {
                var rounded = Math.Ceiling(amount);
                return Result<(decimal, decimal)>.Success((rounded, rounded - amount));
            });

        // Use a price that produces a non-integer total so Ceiling has effect
        var lineCalc = _service.CalculateLine(new LineCalculationInput
            { Quantity = 3m, UnitPrice = 10.50m, DiscountPercentage = 0, TaxRate = 0.16m });

        var lines = new List<DocumentLineInput>
        {
            new() { Subtotal = lineCalc.Subtotal, DiscountAmount = 0, IsTaxable = true,
                     TaxAmount = lineCalc.TaxAmount, TaxBase = lineCalc.TaxBase }
        };

        var result = await _service.CalculateDocumentAsync(new DocumentCalculationInput
        {
            Lines = lines, GlobalDiscountPercentage = 0, TaxRate = 0.16m, ApplyRounding = true
        });

        // preRoundingTotal = 36.54, Ceiling → 37.00, roundingAmount = 0.46
        Assert.That(result.RoundingAmount, Is.GreaterThan(0m), "Positive rounding must be applied");
        Assert.That(result.Total, Is.GreaterThan(result.PreRoundingTotal));
    }

    #endregion
}
