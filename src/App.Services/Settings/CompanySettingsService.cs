using AutoMapper;

using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class CompanySettingsService : ICompanySettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CompanySettingsService> _logger;
    private readonly IStringLocalizer<CompanySettingsService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public CompanySettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CompanySettingsService> logger,
        IStringLocalizer<CompanySettingsService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<CompanySettingsDto?> GetSettingsAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var settings = await _context.CompanySettings
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return settings != null ? _mapper.Map<CompanySettingsDto>(settings) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company settings");
            throw;
        }
    }

    private const string DefaultTimeZoneId = "America/Mexico_City";

    public async Task<CompanySettingsDto> UpdateSettingsAsync(UpdateCompanySettingsDto updateDto)
    {
        try
        {
            if (!IsValidTimeZone(updateDto.TimeZoneId))
            {
                throw new InvalidOperationException(
                    _localizer["Invalid time zone ID: {0}", updateDto.TimeZoneId]);
            }

            await using var _context = await _contextFactory.CreateDbContextAsync();

            var settings = await _context.CompanySettings
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new CompanySettings
                {
                    CreatedBy = _currentUserService.FullName ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                _context.CompanySettings.Add(settings);
            }

            _mapper.Map(updateDto, settings);

            // Persist the display name alongside the IANA/Windows timezone ID
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(updateDto.TimeZoneId);
                settings.TimeZoneDisplayName = tz.DisplayName;
            }
            catch
            {
                settings.TimeZoneDisplayName = updateDto.TimeZoneId;
            }

            settings.ModifiedBy = _currentUserService.FullName ?? "Unknown";
            settings.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Company settings updated by {User}. Timezone set to: {TimeZoneId}",
                _currentUserService.FullName, settings.TimeZoneId);

            return _mapper.Map<CompanySettingsDto>(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company settings");
            throw;
        }
    }

    public async Task<TimeZoneInfo> GetCurrentTimeZoneAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            var timeZoneId = settings?.TimeZoneId;

            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning(
                        "Configured timezone '{TimeZoneId}' not found. Falling back to {Default}",
                        timeZoneId, DefaultTimeZoneId);
                }
            }

            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current time zone, falling back to {Default}", DefaultTimeZoneId);
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
    }

    public bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating timezone: {TimeZoneId}", timeZoneId);
            return false;
        }
    }
}