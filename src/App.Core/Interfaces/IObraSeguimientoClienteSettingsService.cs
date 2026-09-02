using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface IObraSeguimientoClienteSettingsService
{
    /// <summary>Gets the current settings, or the 90-day default if none are stored yet.</summary>
    Task<ObraSeguimientoClienteSettingsDto> GetSettingsAsync();

    Task<ObraSeguimientoClienteSettingsDto> UpdateSettingsAsync(UpdateObraSeguimientoClienteSettingsDto updateDto);
}
