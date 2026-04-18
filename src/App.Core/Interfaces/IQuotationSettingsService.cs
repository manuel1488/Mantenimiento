using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface IQuotationSettingsService
{
    Task<QuotationSettingsDto> GetSettingsAsync();
    Task<QuotationSettingsDto> SaveSettingsAsync(QuotationSettingsDto dto);
}
