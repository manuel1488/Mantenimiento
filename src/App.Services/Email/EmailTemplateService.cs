using App.Core.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;

using Scriban;

using System.Globalization;

namespace App.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly IFileProvider _fileProvider;
    private const string TemplatesPath = "EmailTemplates";

    public EmailTemplateService(
        ILogger<EmailTemplateService> logger,
        IFileProvider fileProvider)
    {
        _logger = logger;
        _fileProvider = fileProvider;
    }

    public async Task<string> GetTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extraer la cultura del diccionario de datos o usar la cultura actual
            string cultureName = "en"; // Cultura predeterminada

            if (data is Dictionary<string, object> dictionary &&
                dictionary.TryGetValue("culture", out var cultureObj) &&
                cultureObj is string culture)
            {
                cultureName = culture;
            }
            else
            {
                // Fallback a la cultura de UI actual
                cultureName = CultureInfo.CurrentUICulture.Name;
            }

            // Primero intentar con cultura específica (es-MX, es-ES, etc.)
            string localizedTemplatePath = Path.Combine(TemplatesPath, $"{templateName}.{cultureName}.html");
            IFileInfo fileInfo = _fileProvider.GetFileInfo(localizedTemplatePath);

            // Si no existe, intentar con cultura general (es, fr, etc.)
            if (!fileInfo.Exists && cultureName.Contains('-'))
            {
                string generalCulture = cultureName.Split('-')[0];
                localizedTemplatePath = Path.Combine(TemplatesPath, $"{templateName}.{generalCulture}.html");
                fileInfo = _fileProvider.GetFileInfo(localizedTemplatePath);
            }

            // Si aún no existe, usar plantilla predeterminada
            if (!fileInfo.Exists)
            {
                string defaultTemplatePath = Path.Combine(TemplatesPath, $"{templateName}.html");
                fileInfo = _fileProvider.GetFileInfo(defaultTemplatePath);
            }

            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Template {templateName} not found");

            _logger.LogInformation("Using template: {TemplatePath}", fileInfo.PhysicalPath ?? fileInfo.Name);

            using var reader = new StreamReader(fileInfo.CreateReadStream());
            var templateContent = await reader.ReadToEndAsync();

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
                    // Extraer nombre base de la plantilla (sin cultura)
                    string fileName = file.Name;
                    int dotIndex = fileName.IndexOf('.');

                    // Si tiene formato templateName.culture.html, extraer solo templateName
                    if (dotIndex > 0 && fileName.Substring(dotIndex + 1).Contains('.'))
                    {
                        templates.Add(fileName.Substring(0, dotIndex));
                    }
                    // Si tiene formato templateName.html
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
}