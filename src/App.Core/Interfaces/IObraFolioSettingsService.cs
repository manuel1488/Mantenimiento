using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface IObraFolioSettingsService
{
    /// <summary>Gets the current Obra folio settings, or the "OBR"/4-digit defaults if none are stored yet.</summary>
    Task<ObraFolioSettingsDto> GetSettingsAsync();

    Task<ObraFolioSettingsDto> UpdateSettingsAsync(UpdateObraFolioSettingsDto updateDto);
}
