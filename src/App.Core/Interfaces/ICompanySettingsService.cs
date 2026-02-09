using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface ICompanySettingsService
{
    /// <summary>
    /// Gets the current company settings
    /// </summary>
    Task<CompanySettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Updates the company settings
    /// </summary>
    Task<CompanySettingsDto> UpdateSettingsAsync(UpdateCompanySettingsDto updateDto);

    /// <summary>
    /// Gets the current time zone configuration
    /// </summary>
    Task<TimeZoneInfo?> GetCurrentTimeZoneAsync();
}