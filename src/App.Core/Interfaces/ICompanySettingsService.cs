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
    /// Gets the current time zone. Falls back to America/Mexico_City if the configured
    /// timezone is missing or invalid.
    /// </summary>
    Task<TimeZoneInfo> GetCurrentTimeZoneAsync();

    /// <summary>
    /// Returns true if the given timezone ID is recognized by the runtime
    /// (accepts both Windows and IANA IDs).
    /// </summary>
    bool IsValidTimeZone(string timeZoneId);
}