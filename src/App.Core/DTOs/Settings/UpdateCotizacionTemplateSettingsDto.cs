using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateCotizacionTemplateSettingsDto
{
    [Required]
    public string HtmlContent { get; set; } = null!;

    public string CssContent { get; set; } = string.Empty;
}
