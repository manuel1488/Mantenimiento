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

    /// <summary>
    /// Gets the main brand logo as a ready-to-use data URI (e.g. "data:image/png;base64,...").
    /// This is the general, full-color logo shown in the NavMenu, Login screen, and business
    /// documents (quotations, remissions, transfers, counts, sales reports) — distinct from the
    /// ticket-specific logo configured in Settings &gt; Tickets, which may be a simplified/B&amp;W
    /// variant optimized for thermal printing.
    /// Returns null if no logo has been configured yet.
    /// </summary>
    Task<string?> GetLogoDataUriAsync();
}