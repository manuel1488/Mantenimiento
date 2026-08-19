using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface ICotizacionTemplateSettingsService
{
    /// <summary>Gets the DB override row, or null if none has been saved yet.</summary>
    Task<CotizacionTemplateSettingsDto?> GetConfigAsync();

    /// <summary>Creates or updates the singleton DB override.</summary>
    Task<CotizacionTemplateSettingsDto> UpdateConfigAsync(UpdateCotizacionTemplateSettingsDto dto);

    /// <summary>Deletes the DB override, reverting to the on-disk default template.</summary>
    Task ResetAsync();

    /// <summary>
    /// Returns the (HtmlContent, CssContent) that should actually be rendered: the DB override
    /// if one exists, otherwise the on-disk default template split into body/css.
    /// </summary>
    Task<(string HtmlContent, string CssContent)> GetEffectiveTemplateAsync();
}
