using App.Core.Common;
using App.Core.DTOs.Shop.Calculation;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Services.Settings;

using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class PricingCalculationService : IPricingCalculationService
{
    private readonly ITaxRateService _taxRateService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IRoundingSettingsService _roundingSettingsService;
    private readonly ILogger<PricingCalculationService> _logger;

    public PricingCalculationService(
        ITaxRateService taxRateService,
        ICompanySettingsService companySettingsService,
        IRoundingSettingsService roundingSettingsService,
        ILogger<PricingCalculationService> logger)
    {
        _taxRateService = taxRateService;
        _companySettingsService = companySettingsService;
        _roundingSettingsService = roundingSettingsService;
        _logger = logger;
    }

    public LineCalculationResult CalculateLine(LineCalculationInput input)
    {
        if (input.HasCustomTotal && input.CustomTotal.HasValue)
        {
            return new LineCalculationResult
            {
                BasePriceBeforeSurcharge = input.Quantity * input.UnitPrice,
                DiscountAmount = 0,
                SurchargeAmount = 0,
                Subtotal = input.CustomTotal.Value,
                Total = input.CustomTotal.Value,
                GrossAmount = input.CustomTotal.Value,
                TaxBase = input.CustomTotal.Value,
                TaxAmount = Math.Round(input.CustomTotal.Value * input.TaxRate, 2),
                TaxRate = input.TaxRate
            };
        }

        // No rounding at item level — keep full precision for consistent display
        decimal basePriceBeforeSurcharge = input.Quantity * input.UnitPrice;
        decimal discountAmount = basePriceBeforeSurcharge * (input.DiscountPercentage / 100);
        decimal afterDiscount = basePriceBeforeSurcharge - discountAmount;
        decimal surchargeAmount = afterDiscount * (input.SurchargePercentage / 100);
        decimal subtotal = afterDiscount + surchargeAmount;

        // Line-level values keep 6-decimal precision so that rounding errors
        // don't accumulate (e.g. $6.034483 × 1.16 must yield $7.00, not $6.99).
        // Document-level totals round to 2 decimals for currency compliance.
        // CFDI XML generation (MexicoInvoiceService) applies its own 2-decimal
        // rounding per SAT Anexo 20 independently.
        decimal grossAmount = Math.Round(input.Quantity * input.UnitPrice, 6);
        decimal roundedDiscount = Math.Round(discountAmount, 6);
        decimal taxBase = grossAmount - roundedDiscount;
        decimal taxAmount = input.TaxRate > 0 ? Math.Round(taxBase * input.TaxRate, 6) : 0;

        return new LineCalculationResult
        {
            BasePriceBeforeSurcharge = basePriceBeforeSurcharge,
            DiscountAmount = discountAmount,
            SurchargeAmount = surchargeAmount,
            Subtotal = subtotal,
            Total = subtotal,
            GrossAmount = grossAmount,
            TaxBase = taxBase,
            TaxAmount = taxAmount,
            TaxRate = input.TaxRate
        };
    }

    public async Task<DocumentCalculationResult> CalculateDocumentAsync(DocumentCalculationInput input)
    {
        // All document-level amounts rounded to 2 decimals for CFDI compliance
        decimal subtotal = Math.Round(input.Lines.Sum(l => l.Subtotal + l.DiscountAmount), 2);
        decimal itemDiscounts = Math.Round(input.Lines.Sum(l => l.DiscountAmount), 2);

        decimal netAfterItemDiscounts = subtotal - itemDiscounts;
        decimal globalDiscount = Math.Round(netAfterItemDiscounts * (input.GlobalDiscountPercentage / 100), 2);
        decimal totalDiscount = Math.Round(itemDiscounts + globalDiscount, 2);

        // Tax calculation: sum of per-line rounded amounts (CFDI-compliant).
        // Each line's TaxAmount MUST be pre-computed as Round(TaxBase × TaxRate, 2) via CalculateLine.
        // Fail fast if a taxable line is missing tax data — silent $0 tax causes payment mismatches.
        if (input.TaxRate > 0)
        {
            foreach (var line in input.Lines)
            {
                if (line.IsTaxable && line.TaxAmount == 0 && line.TaxBase == 0 && line.Subtotal > 0)
                    throw new InvalidOperationException(
                        $"Taxable line (Subtotal={line.Subtotal}) has TaxAmount=0 and TaxBase=0. " +
                        "Use CalculateLine with TaxRate to pre-compute tax values before calling CalculateDocumentAsync.");
            }
        }

        decimal taxAmount;
        if (globalDiscount > 0 && input.TaxRate > 0)
        {
            // With global discount: distribute it proportionally to each line's tax base,
            // then recalculate per-line tax with the adjusted base.
            taxAmount = input.Lines.Sum(line =>
            {
                if (!line.IsTaxable || line.TaxBase <= 0 || netAfterItemDiscounts <= 0) return 0m;
                decimal proportion = line.Subtotal / netAfterItemDiscounts;
                decimal lineGlobalDiscount = Math.Round(globalDiscount * proportion, 2);
                decimal adjustedBase = line.TaxBase - lineGlobalDiscount;
                return adjustedBase > 0 ? Math.Round(adjustedBase * input.TaxRate, 2) : 0m;
            });
        }
        else
        {
            // No global discount: use pre-computed per-line tax amounts directly.
            taxAmount = input.Lines.Sum(l => l.TaxAmount);
        }

        // Round tax to 2 decimals at document level (currency precision)
        taxAmount = Math.Round(taxAmount, 2);
        decimal preRoundingTotal = Math.Round(subtotal - totalDiscount + taxAmount, 2);

        // Apply rounding if enabled
        decimal roundingAmount = 0;
        decimal total = preRoundingTotal;

        if (input.ApplyRounding)
        {
            var roundingResult = await _roundingSettingsService.ApplyRoundingAsync(preRoundingTotal);
            if (roundingResult.IsSuccess && roundingResult.Value.RoundingAmount > 0)
            {
                // Only apply positive rounding (customer pays more).
                // Negative rounding (Floor) is not applied — CFDI cannot
                // reduce the total via a non-taxable concepto without
                // cascading tax base changes.
                roundingAmount = Math.Round(roundingResult.Value.RoundingAmount, 2);
                total = Math.Round(roundingResult.Value.RoundedTotal, 2);
            }
        }

        return new DocumentCalculationResult
        {
            Subtotal = subtotal,
            ItemDiscountAmount = itemDiscounts,
            GlobalDiscountAmount = globalDiscount,
            TotalDiscountAmount = totalDiscount,
            TaxAmount = taxAmount,
            PreRoundingTotal = preRoundingTotal,
            RoundingAmount = roundingAmount,
            Total = total
        };
    }

    public async Task<Result<decimal>> GetEffectiveTaxRateAsync()
    {
        try
        {
            var companySettings = await _companySettingsService.GetSettingsAsync();
            if (companySettings == null)
                return Result<decimal>.Failure("Company settings not configured");

            var rate = await _taxRateService.GetEffectiveRateAsync(companySettings.CountryCode);
            return Result<decimal>.Success(rate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective tax rate");
            return Result<decimal>.Failure("Error getting tax rate");
        }
    }
}
