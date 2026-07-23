using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Enums.Settings;
using App.Core.Interfaces.Settings;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class RoundingSettingsService : IRoundingSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<RoundingSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IStringLocalizer<RoundingSettingsService> L;

    public RoundingSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<RoundingSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IStringLocalizer<RoundingSettingsService> localizer)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        L = localizer;
    }

    public async Task<Result<RoundingSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.RoundingSettings
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                // Return default settings if none exist
                return Result<RoundingSettingsDto>.Success(new RoundingSettingsDto
                {
                    IsEnabled = false,
                    Method = RoundingMethod.Ceiling,
                    DecimalPlaces = 0,
                    MinimumThreshold = 0
                });
            }

            var dto = _mapper.Map<RoundingSettingsDto>(settings);
            return Result<RoundingSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rounding settings");
            return Result<RoundingSettingsDto>.Failure(L["An error occurred while retrieving rounding settings"]);
        }
    }

    public async Task<Result<RoundingSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateRoundingSettingsDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the settings first
            var validationResult = ValidateSettings(updateDto);
            if (!validationResult.IsSuccess)
            {
                return Result<RoundingSettingsDto>.Failure(validationResult.Error!);
            }

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var settings = await context.RoundingSettings
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                // Create new settings
                settings = new RoundingSettings
                {
                    IsEnabled = updateDto.IsEnabled,
                    Method = updateDto.Method,
                    DecimalPlaces = updateDto.DecimalPlaces,
                    MinimumThreshold = updateDto.MinimumThreshold,
                    CreatedBy = await _currentUserService.GetFullNameAsync(),
                    CreatedAt = _dateTime.Now
                };

                context.RoundingSettings.Add(settings);
            }
            else
            {
                // Update existing settings
                settings.IsEnabled = updateDto.IsEnabled;
                settings.Method = updateDto.Method;
                settings.DecimalPlaces = updateDto.DecimalPlaces;
                settings.MinimumThreshold = updateDto.MinimumThreshold;
                settings.ModifiedBy = await _currentUserService.GetFullNameAsync();
                settings.ModifiedAt = _dateTime.Now;
            }

            await context.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<RoundingSettingsDto>(settings);
            return Result<RoundingSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rounding settings");
            return Result<RoundingSettingsDto>.Failure(L["An error occurred while saving rounding settings"]);
        }
    }

    public async Task<Result<decimal>> CalculateRoundingAsync(
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var result = await ApplyRoundingAsync(amount, cancellationToken);
        if (!result.IsSuccess)
            return Result<decimal>.Failure(result.Error!);

        return Result<decimal>.Success(result.Value.RoundingAmount);
    }

    public async Task<Result<(decimal RoundedTotal, decimal RoundingAmount)>> ApplyRoundingAsync(
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settingsResult = await GetSettingsAsync(cancellationToken);
            if (!settingsResult.IsSuccess)
                return Result<(decimal, decimal)>.Failure(settingsResult.Error!);

            var settings = settingsResult.Value!;

            // If disabled or below threshold, return original amount
            if (!settings.IsEnabled || amount < settings.MinimumThreshold)
                return Result<(decimal, decimal)>.Success((amount, 0));

            decimal roundedTotal = settings.Method switch
            {
                RoundingMethod.Ceiling => RoundCeiling(amount, settings.DecimalPlaces),
                RoundingMethod.Floor => RoundFloor(amount, settings.DecimalPlaces),
                RoundingMethod.Nearest => Math.Round(amount, settings.DecimalPlaces, MidpointRounding.AwayFromZero),
                _ => amount
            };

            decimal roundingAmount = roundedTotal - amount;
            return Result<(decimal, decimal)>.Success((roundedTotal, roundingAmount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying rounding to amount {Amount}", amount);
            return Result<(decimal, decimal)>.Failure(L["An error occurred while calculating rounding"]);
        }
    }

    private static decimal RoundCeiling(decimal value, int decimals)
    {
        decimal multiplier = (decimal)Math.Pow(10, decimals);
        return Math.Ceiling(value * multiplier) / multiplier;
    }

    private static decimal RoundFloor(decimal value, int decimals)
    {
        decimal multiplier = (decimal)Math.Pow(10, decimals);
        return Math.Floor(value * multiplier) / multiplier;
    }

    private Result ValidateSettings(UpdateRoundingSettingsDto settings)
    {
        if (settings.DecimalPlaces < 0 || settings.DecimalPlaces > 2)
        {
            return Result.Failure(L["Decimal places must be between 0 and 2"]);
        }

        if (settings.MinimumThreshold < 0)
        {
            return Result.Failure(L["Minimum threshold cannot be negative"]);
        }

        return Result.Success();
    }
}
