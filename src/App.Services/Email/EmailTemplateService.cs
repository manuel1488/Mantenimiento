using App.Core.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;

using Scriban;

using System.Globalization;
using System.Text.RegularExpressions;

namespace App.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly IFileProvider _fileProvider;
    private readonly IEmailTemplateSettingsService _templateSettingsService;
    private const string TemplatesPath = "EmailTemplates";

    public EmailTemplateService(
        ILogger<EmailTemplateService> logger,
        IFileProvider fileProvider,
        IEmailTemplateSettingsService templateSettingsService)
    {
        _logger = logger;
        _fileProvider = fileProvider;
        _templateSettingsService = templateSettingsService;
    }

    public async Task<string> GetTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            string cultureName = "en";

            if (data is Dictionary<string, object> dictionary &&
                dictionary.TryGetValue("culture", out var cultureObj) &&
                cultureObj is string culture)
            {
                cultureName = culture;
            }
            else
            {
                cultureName = CultureInfo.CurrentUICulture.Name;
            }

            // Normalize to base language code (e.g. "es-MX" → "es")
            string baseLanguage = cultureName.Contains('-') ? cultureName.Split('-')[0] : cultureName;

            // 1. Check DB override (exact language first, then base)
            string templateContent = await GetDbTemplateAsync(templateName, cultureName)
                ?? await GetDbTemplateAsync(templateName, baseLanguage)
                ?? await GetFileTemplateAsync(templateName, cultureName)
                ?? throw new FileNotFoundException($"Template {templateName} not found");

            var template = Template.Parse(templateContent);
            var result = await template.RenderAsync(data);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading template {TemplateName}", templateName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = new HashSet<string>();
            var directoryContents = _fileProvider.GetDirectoryContents(TemplatesPath);

            foreach (var file in directoryContents)
            {
                if (file.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    string fileName = file.Name;
                    int dotIndex = fileName.IndexOf('.');

                    if (dotIndex > 0 && fileName.Substring(dotIndex + 1).Contains('.'))
                    {
                        templates.Add(fileName.Substring(0, dotIndex));
                    }
                    else if (dotIndex > 0)
                    {
                        templates.Add(Path.GetFileNameWithoutExtension(fileName));
                    }
                }
            }

            return await Task.FromResult(templates.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available templates");
            throw;
        }
    }

    private async Task<string?> GetDbTemplateAsync(string name, string language)
    {
        var result = await _templateSettingsService.GetAsync(name);
        if (!result.IsSuccess) return null;

        _logger.LogInformation("Using DB override for template {Name}", name);
        var dto = result.Value;

        // Legacy entry: full HTML stored in HtmlContent (no CSS separation yet)
        if (string.IsNullOrEmpty(dto.CssContent) &&
            dto.HtmlContent.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            return dto.HtmlContent;
        }

        // Modern: combine CSS + body into full HTML page
        return BuildFullHtml(dto.HtmlContent, dto.CssContent);
    }

    private static string BuildFullHtml(string htmlBody, string cssContent) =>
        $"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <style>
        {cssContent}
        </style>
        </head>
        <body>
        {htmlBody}
        </body>
        </html>
        """;

    /// <summary>Extracts (body, css) from a complete HTML file string.</summary>
    public static (string Body, string Css) ExtractCssAndBody(string fullHtml)
    {
        var styleMatch = Regex.Match(fullHtml, @"<style[^>]*>(.*?)</style>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var css = styleMatch.Success ? styleMatch.Groups[1].Value.Trim() : string.Empty;

        var bodyMatch = Regex.Match(fullHtml, @"<body[^>]*>(.*?)</body>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var body = bodyMatch.Success ? bodyMatch.Groups[1].Value.Trim() : fullHtml;

        return (body, css);
    }

    private async Task<string?> GetFileTemplateAsync(string templateName, string cultureName)
    {
        // Try specific culture (e.g. es-MX)
        string path = Path.Combine(TemplatesPath, $"{templateName}.{cultureName}.html");
        IFileInfo fileInfo = _fileProvider.GetFileInfo(path);

        // Try base language (e.g. es)
        if (!fileInfo.Exists && cultureName.Contains('-'))
        {
            string general = cultureName.Split('-')[0];
            path = Path.Combine(TemplatesPath, $"{templateName}.{general}.html");
            fileInfo = _fileProvider.GetFileInfo(path);
        }

        // Try default (no language suffix)
        if (!fileInfo.Exists)
        {
            path = Path.Combine(TemplatesPath, $"{templateName}.html");
            fileInfo = _fileProvider.GetFileInfo(path);
        }

        if (!fileInfo.Exists)
            return null;

        _logger.LogInformation("Using file template: {Path}", fileInfo.PhysicalPath ?? fileInfo.Name);

        using var reader = new StreamReader(fileInfo.CreateReadStream());
        return await reader.ReadToEndAsync();
    }
}
