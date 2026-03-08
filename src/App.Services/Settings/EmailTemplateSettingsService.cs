using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class EmailTemplateSettingsService : IEmailTemplateSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<EmailTemplateSettingsService> _logger;
    private readonly IStringLocalizer<EmailTemplateSettingsService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public EmailTemplateSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<EmailTemplateSettingsService> logger,
        IStringLocalizer<EmailTemplateSettingsService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Result<EmailTemplateSettingsDto>> GetAsync(string name)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.EmailTemplateSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == name);

            if (entity == null)
                return Result<EmailTemplateSettingsDto>.Failure(_localizer["Template not found"]);

            return Result<EmailTemplateSettingsDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template {Name}", name);
            return Result<EmailTemplateSettingsDto>.Failure(_localizer["Error retrieving template"]);
        }
    }

    public async Task<Result<EmailTemplateSettingsDto>> SaveAsync(SaveEmailTemplateSettingsDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = _dateTime.Now;
            var user = _currentUserService.UserId ?? "System";

            var entity = await context.EmailTemplateSettings
                .FirstOrDefaultAsync(t => t.Name == dto.Name);

            if (entity == null)
            {
                entity = new EmailTemplateSettings
                {
                    Name = dto.Name,
                    HtmlContent = dto.HtmlContent,
                    CssContent = dto.CssContent,
                    CreatedBy = user,
                    CreatedAt = now,
                    ModifiedBy = user,
                    ModifiedAt = now
                };
                context.EmailTemplateSettings.Add(entity);
            }
            else
            {
                entity.HtmlContent = dto.HtmlContent;
                entity.CssContent = dto.CssContent;
                entity.ModifiedBy = user;
                entity.ModifiedAt = now;
            }

            await context.SaveChangesAsync();
            return Result<EmailTemplateSettingsDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving email template {Name}", dto.Name);
            return Result<EmailTemplateSettingsDto>.Failure(_localizer["Error saving template"]);
        }
    }

    public async Task<Result> DeleteAsync(string name)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.EmailTemplateSettings
                .FirstOrDefaultAsync(t => t.Name == name);

            if (entity == null)
                return Result.Failure(_localizer["Template not found"]);

            entity.DeletedBy = _currentUserService.UserId;
            entity.DeletedAt = _dateTime.Now;
            context.EmailTemplateSettings.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email template {Name}", name);
            return Result.Failure(_localizer["Error deleting template"]);
        }
    }

    private static EmailTemplateSettingsDto MapToDto(EmailTemplateSettings entity) =>
        new() { Id = entity.Id, Name = entity.Name, HtmlContent = entity.HtmlContent, CssContent = entity.CssContent };
}
