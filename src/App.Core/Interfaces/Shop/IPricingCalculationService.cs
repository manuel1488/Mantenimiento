using App.Core.Common;
using App.Core.DTOs.Shop.Calculation;

namespace App.Core.Interfaces.Shop;

public interface IPricingCalculationService
{
    /// <summary>
    /// Calculates line-item totals. Pure math, no rounding — keeps full precision for display.
    /// </summary>
    LineCalculationResult CalculateLine(LineCalculationInput input);

    /// <summary>
    /// Calculates document-level totals with rounding to 2 decimals (CFDI compliant).
    /// </summary>
    Task<DocumentCalculationResult> CalculateDocumentAsync(DocumentCalculationInput input);

    /// <summary>
    /// Gets the effective tax rate as a fraction (e.g. 0.16 for 16%) for the configured country.
    /// </summary>
    Task<Result<decimal>> GetEffectiveTaxRateAsync();
}
