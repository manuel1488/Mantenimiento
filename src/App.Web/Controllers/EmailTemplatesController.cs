using App.Core.DTOs.Settings;
using App.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[ApiController]
[Route("api/email-templates")]
[Authorize]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailTemplateSettingsService _emailTemplateSettingsService;
    private readonly ILogger<EmailTemplatesController> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmailTemplatesController(
        IEmailTemplateService emailTemplateService,
        IEmailTemplateSettingsService emailTemplateSettingsService,
        ILogger<EmailTemplatesController> logger,
        IWebHostEnvironment environment)
    {
        _emailTemplateService = emailTemplateService;
        _emailTemplateSettingsService = emailTemplateSettingsService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>Renders the saved template (DB or file) with sample data.</summary>
    [HttpGet("{name}/preview")]
    public async Task<IActionResult> Preview([FromRoute] string name, [FromQuery] string lang = "es")
    {
        try
        {
            var sampleData = GetSampleData(name, lang);
            var html = await _emailTemplateService.GetTemplateAsync(name, sampleData);
            return Content(html, "text/html");
        }
        catch (FileNotFoundException)
        {
            return NotFound($"Template '{name}' not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering preview for template {Name}", name);
            return StatusCode(500, "Error rendering template preview.");
        }
    }

    /// <summary>Renders arbitrary HTML content with sample data and returns the result.</summary>
    [HttpPost("{name}/render-preview")]
    public async Task<IActionResult> RenderPreview([FromRoute] string name, [FromBody] RenderPreviewRequest request)
    {
        try
        {
            var sampleData = GetSampleData(name, request.Language ?? "es");
            var template = Scriban.Template.Parse(request.HtmlContent);
            var html = await template.RenderAsync(sampleData);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering preview for template {Name}", name);
            return StatusCode(500, "Error rendering preview.");
        }
    }

    /// <summary>Returns the raw HTML of a preset file.</summary>
    [HttpGet("presets/{presetKey}")]
    public async Task<IActionResult> GetPreset([FromRoute] string presetKey)
    {
        var allowed = new[] { "classic", "compact", "modern" };
        if (!allowed.Contains(presetKey))
            return BadRequest("Invalid preset key.");

        var path = Path.Combine(
            _environment.WebRootPath, "EmailTemplates", "Presets", $"{presetKey}.html");

        if (!System.IO.File.Exists(path))
            return NotFound($"Preset '{presetKey}' not found.");

        var html = await System.IO.File.ReadAllTextAsync(path);
        return Content(html, "text/plain");
    }

    private static Dictionary<string, object> GetSampleData(string templateName, string language) =>
        App.Web.Components.Admin.Settings.Email.EmailTemplateSampleData.GetSampleData(templateName, language);
}

public class RenderPreviewRequest
{
    public string HtmlContent { get; set; } = string.Empty;
    public string? Language { get; set; }
}
