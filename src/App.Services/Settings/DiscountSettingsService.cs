using AutoMapper;
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

public class DiscountSettingsService : IDiscountSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<DiscountSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IStringLocalizer<DiscountSettingsService> L;

    public DiscountSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<DiscountSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IStringLocalizer<DiscountSettingsService> localizer)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        L = localizer;
    }

    public async Task<Result<DiscountSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.DiscountSettings
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                return Result<DiscountSettingsDto>.Failure(L["Discount settings not found"]);
            }

            var dto = _mapper.Map<DiscountSettingsDto>(settings);
            return Result<DiscountSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount settings");
            return Result<DiscountSettingsDto>.Failure(L["An error occurred while retrieving discount settings"]);
        }
    }

    public async Task<Result<DiscountSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateDiscountSettingsDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = ValidateSettings(updateDto);
            if (!validationResult.IsSuccess)
            {
                return Result<DiscountSettingsDto>.Failure(validationResult.Error!);
            }

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.DiscountSettings
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                settings = new DiscountSettings
                {
                    RequireAuthorizationForPublicDiscount = updateDto.RequireAuthorizationForPublicDiscount,
                    MaximumPublicDiscount = updateDto.MaximumPublicDiscount,
                    CreatedBy = _currentUserService.FullName,
                    CreatedAt = _dateTime.Now
                };

                context.DiscountSettings.Add(settings);
            }
            else
            {
                settings.RequireAuthorizationForPublicDiscount = updateDto.RequireAuthorizationForPublicDiscount;
                settings.MaximumPublicDiscount = updateDto.MaximumPublicDiscount;
                settings.ModifiedBy = _currentUserService.FullName;
                settings.ModifiedAt = _dateTime.Now;
            }

            await context.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<DiscountSettingsDto>(settings);
            return Result<DiscountSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving discount settings");
            return Result<DiscountSettingsDto>.Failure(L["An error occurred while saving discount settings"]);
        }
    }

    public async Task<Result<bool>> ValidateDiscountAsync(
        decimal discountPercentage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settingsResult = await GetSettingsAsync(cancellationToken);
            if (!settingsResult.IsSuccess)
            {
                return Result<bool>.Failure(settingsResult.Error!);
            }

            var settings = settingsResult.Value!;

            if (discountPercentage > settings.MaximumPublicDiscount)
            {
                return Result<bool>.Failure(
                    L["The discount exceeds the maximum allowed for public sales ({0}%)",
                        settings.MaximumPublicDiscount]);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating discount");
            return Result<bool>.Failure(L["An error occurred while validating the discount"]);
        }
    }

    public async Task<Result<bool>> RequiresAuthorizationAsync(
        decimal discountPercentage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settingsResult = await GetSettingsAsync(cancellationToken);
            if (!settingsResult.IsSuccess)
            {
                return Result<bool>.Failure(settingsResult.Error!);
            }

            var settings = settingsResult.Value!;

            if (settings.RequireAuthorizationForPublicDiscount && discountPercentage > 0)
            {
                return Result<bool>.Success(true);
            }

            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if discount requires authorization");
            return Result<bool>.Failure(L["An error occurred while checking discount authorization requirements"]);
        }
    }

    private Result ValidateSettings(UpdateDiscountSettingsDto settings)
    {
        if (settings.MaximumPublicDiscount < 0 || settings.MaximumPublicDiscount > 100)
        {
            return Result.Failure(L["Maximum public discount must be between 0 and 100"]);
        }

        return Result.Success();
    }
}
