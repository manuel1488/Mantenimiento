using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Interfaces.Settings;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class LabelSettingsService : ILabelSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ILogger<LabelSettingsService> _logger;
    private readonly IStringLocalizer<LabelSettingsService> _localizer;

    public LabelSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ILogger<LabelSettingsService> logger,
        IStringLocalizer<LabelSettingsService> localizer)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<Result<LabelSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.LabelSettings
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
                return Result<LabelSettingsDto>.Success(new LabelSettingsDto { WidthMm = 62, HeightMm = 28 });

            return Result<LabelSettingsDto>.Success(new LabelSettingsDto
            {
                Id = settings.Id,
                WidthMm = settings.WidthMm,
                HeightMm = settings.HeightMm
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving label settings");
            return Result<LabelSettingsDto>.Failure(_localizer["Error retrieving label settings"]);
        }
    }

    public async Task<Result<LabelSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateLabelSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.LabelSettings
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var currentUser = _currentUserService.UserId ?? "System";
            var now = _dateTime.Now;

            if (settings == null)
            {
                settings = new LabelSettings
                {
                    WidthMm = dto.WidthMm,
                    HeightMm = dto.HeightMm,
                    CreatedBy = currentUser,
                    CreatedAt = now,
                    ModifiedBy = currentUser,
                    ModifiedAt = now
                };
                context.LabelSettings.Add(settings);
            }
            else
            {
                settings.WidthMm = dto.WidthMm;
                settings.HeightMm = dto.HeightMm;
                settings.ModifiedBy = currentUser;
                settings.ModifiedAt = now;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result<LabelSettingsDto>.Success(new LabelSettingsDto
            {
                Id = settings.Id,
                WidthMm = settings.WidthMm,
                HeightMm = settings.HeightMm
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving label settings");
            return Result<LabelSettingsDto>.Failure(_localizer["Error saving label settings"]);
        }
    }
}
