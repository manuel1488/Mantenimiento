namespace App.Core.DTOs.Settings;

public class EmailTemplateSettingsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;
    public string CssContent { get; set; } = string.Empty;
}

public class SaveEmailTemplateSettingsDto
{
    public string Name { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;
    public string CssContent { get; set; } = string.Empty;
}
