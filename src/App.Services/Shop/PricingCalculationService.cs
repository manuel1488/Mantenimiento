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
                Total = input.CustomTotal.Value
            };
        }

        // No rounding at item level — keep full precision for consistent display
        decimal basePriceBeforeSurcharge = input.Quantity * input.UnitPrice;
        decimal discountAmount = basePriceBeforeSurcharge * (input.DiscountPercentage / 100);
        decimal afterDiscount = basePriceBeforeSurcharge - discountAmount;
        decimal surchargeAmount = afterDiscount * (input.SurchargePercentage / 100);
        decimal subtotal = afterDiscount + surchargeAmount;

        return new LineCalculationResult
        {
            BasePriceBeforeSurcharge = basePriceBeforeSurcharge,
            DiscountAmount = discountAmount,
            SurchargeAmount = surchargeAmount,
            Subtotal = subtotal,
            Total = subtotal
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

        decimal taxableBase = subtotal - totalDiscount;

        // Proportional tax calculation across taxable items
        decimal taxAmount = 0;
        if (input.TaxRate > 0 && netAfterItemDiscounts > 0)
        {
            taxAmount = Math.Round(input.Lines.Sum(line =>
            {
                if (!line.IsTaxable) return 0m;
                decimal proportion = line.Subtotal / netAfterItemDiscounts;
                decimal lineBase = taxableBase * proportion;
                return lineBase * input.TaxRate;
            }), 2);
        }

        decimal preRoundingTotal = Math.Round(subtotal - totalDiscount + taxAmount, 2);

        // Apply rounding if enabled
        decimal roundingAmount = 0;
        decimal total = preRoundingTotal;

        if (input.ApplyRounding)
        {
            var roundingResult = await _roundingSettingsService.ApplyRoundingAsync(preRoundingTotal);
            if (roundingResult.IsSuccess)
            {
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
