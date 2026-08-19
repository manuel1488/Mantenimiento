using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Services.Email;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class CotizacionTemplateSettingsService : ICotizacionTemplateSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<CotizacionTemplateSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private const string DefaultTemplatePath = "CotizacionTemplates/default.html";

    public CotizacionTemplateSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFileProvider fileProvider,
        ILogger<CotizacionTemplateSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _fileProvider = fileProvider;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<CotizacionTemplateSettingsDto?> GetConfigAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.CotizacionTemplateSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        return settings == null ? null : MapToDto(settings);
    }

    public async Task<CotizacionTemplateSettingsDto> UpdateConfigAsync(UpdateCotizacionTemplateSettingsDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.CotizacionTemplateSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";
        var currentTime = _dateTime.Now;

        if (settings == null)
        {
            settings = new CotizacionTemplateSettings
            {
                HtmlContent = dto.HtmlContent,
                CssContent = dto.CssContent,
                CreatedBy = currentUser,
                CreatedAt = currentTime
            };
            context.CotizacionTemplateSettings.Add(settings);
        }
        else
        {
            settings.HtmlContent = dto.HtmlContent;
            settings.CssContent = dto.CssContent;
        }

        settings.ModifiedBy = currentUser;
        settings.ModifiedAt = currentTime;

        await context.SaveChangesAsync();

        return MapToDto(settings);
    }

    public async Task ResetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.CotizacionTemplateSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (settings == null) return;

        settings.DeletedBy = await _currentUserService.GetUserIdAsync();
        settings.DeletedAt = _dateTime.Now;
        context.CotizacionTemplateSettings.Remove(settings);
        await context.SaveChangesAsync();
    }

    public async Task<(string HtmlContent, string CssContent)> GetEffectiveTemplateAsync()
    {
        var dbOverride = await GetConfigAsync();
        if (dbOverride != null)
            return (dbOverride.HtmlContent, dbOverride.CssContent);

        var fileInfo = _fileProvider.GetFileInfo(DefaultTemplatePath);
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Default Cotización template not found at {Path}", DefaultTemplatePath);
            return (string.Empty, string.Empty);
        }

        using var reader = new StreamReader(fileInfo.CreateReadStream());
        var fullHtml = await reader.ReadToEndAsync();

        return EmailTemplateService.ExtractCssAndBody(fullHtml);
    }

    private static CotizacionTemplateSettingsDto MapToDto(CotizacionTemplateSettings entity) => new()
    {
        Id = entity.Id,
        HtmlContent = entity.HtmlContent,
        CssContent = entity.CssContent
    };
}
