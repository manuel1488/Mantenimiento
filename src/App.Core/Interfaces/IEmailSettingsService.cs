using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface IEmailSettingsService
{
    /// <summary>
    /// Gets the current email settings if they exist
    /// </summary>
    Task<EmailSettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Updates the email settings or creates them if they don't exist
    /// </summary>
    Task<EmailSettingsDto> UpdateSettingsAsync(UpdateEmailSettingsDto updateDto);
}