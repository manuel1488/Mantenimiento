namespace App.Core.DTOs.Settings;

public class CotizacionTemplateSettingsDto
{
    public int Id { get; set; }
    public string HtmlContent { get; set; } = null!;
    public string CssContent { get; set; } = string.Empty;
}
