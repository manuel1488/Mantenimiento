using App.Core.DTOs.Settings;

namespace App.Services.Settings;

public interface ITaxRateService 
{
    Task<TaxRateDto?> GetByIdAsync(int id);
    Task<IList<TaxRateDto>> GetActiveRatesAsync(string countryCode, string? provinceCode = null);
    Task<IList<TaxRateDto>> GetHistoricalRatesAsync(string countryCode, DateTime startDate, DateTime endDate);
    Task<TaxRateDto> CreateRateAsync(CreateTaxRateDto createDto);
    Task<TaxRateDto> UpdateRateAsync(int id, UpdateTaxRateDto updateDto);
    Task<decimal> GetEffectiveRateAsync(string countryCode, string? provinceCode = null, DateTime? effectiveDate = null);
    Task<bool> DeleteRateAsync(int id);
    Task<bool> ValidateRateAsync(string countryCode, decimal rate);
}