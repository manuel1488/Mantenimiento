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

public class WholesaleSettingsService : IWholesaleSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<WholesaleSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IStringLocalizer<WholesaleSettingsService> L;

    public WholesaleSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<WholesaleSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IStringLocalizer<WholesaleSettingsService> localizer)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        L = localizer;
    }

    public async Task<Result<WholesaleSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.WholesaleSettings
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                return Result<WholesaleSettingsDto>.Success(new WholesaleSettingsDto
                {
                    PriceMode = Core.Enums.Shop.WholesalePriceMode.Percentage,
                    ApplyWholesaleToRemissions = false
                });
            }

            return Result<WholesaleSettingsDto>.Success(new WholesaleSettingsDto
            {
                Id = settings.Id,
                PriceMode = settings.PriceMode,
                ApplyWholesaleToRemissions = settings.ApplyWholesaleToRemissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wholesale settings");
            return Result<WholesaleSettingsDto>.Failure(L["An error occurred while retrieving wholesale settings"]);
        }
    }

    public async Task<Result<WholesaleSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateWholesaleSettingsDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.WholesaleSettings
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                settings = new WholesaleSettings
                {
                    PriceMode = updateDto.PriceMode,
                    ApplyWholesaleToRemissions = updateDto.ApplyWholesaleToRemissions,
                    CreatedBy = _currentUserService.FullName,
                    CreatedAt = _dateTime.Now
                };
                context.WholesaleSettings.Add(settings);
            }
            else
            {
                settings.PriceMode = updateDto.PriceMode;
                settings.ApplyWholesaleToRemissions = updateDto.ApplyWholesaleToRemissions;
                settings.ModifiedBy = _currentUserService.FullName;
                settings.ModifiedAt = _dateTime.Now;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result<WholesaleSettingsDto>.Success(new WholesaleSettingsDto
            {
                Id = settings.Id,
                PriceMode = settings.PriceMode,
                ApplyWholesaleToRemissions = settings.ApplyWholesaleToRemissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving wholesale settings");
            return Result<WholesaleSettingsDto>.Failure(L["An error occurred while saving wholesale settings"]);
        }
    }
}
