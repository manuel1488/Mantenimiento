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

public class EmailSettingsService : IEmailSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<EmailSettingsService> _logger;
    private readonly IStringLocalizer<EmailSettingsService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public EmailSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<EmailSettingsService> logger,
        IStringLocalizer<EmailSettingsService> localizer,
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

    public async Task<EmailSettingsDto?> GetSettingsAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var settings = await _context.EmailSettings
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return settings != null ? _mapper.Map<EmailSettingsDto>(settings) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email settings");
            throw;
        }
    }

    public async Task<EmailSettingsDto> UpdateSettingsAsync(UpdateEmailSettingsDto updateDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Get current settings or create new ones if they don't exist
            var settings = await _context.EmailSettings
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new EmailSettings
                {
                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                _context.EmailSettings.Add(settings);
            }

            // Validate that if SMTP host is provided, port is also provided and vice versa
            if ((!string.IsNullOrEmpty(updateDto.SmtpHost) && !updateDto.SmtpPort.HasValue) ||
                (string.IsNullOrEmpty(updateDto.SmtpHost) && updateDto.SmtpPort.HasValue))
            {
                throw new InvalidOperationException(
                    _localizer["Both SMTP host and port must be provided together"]);
            }

            // Additional validation for credentials
            if (!string.IsNullOrEmpty(updateDto.SmtpUser) && string.IsNullOrEmpty(updateDto.SmtpPassword))
            {
                throw new InvalidOperationException(
                    _localizer["SMTP password is required when user is provided"]);
            }

            if (string.IsNullOrEmpty(updateDto.SmtpUser) && !string.IsNullOrEmpty(updateDto.SmtpPassword))
            {
                throw new InvalidOperationException(
                    _localizer["SMTP user is required when password is provided"]);
            }

            // Update properties
            _mapper.Map(updateDto, settings);

            // Update audit fields
            settings.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            settings.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            return _mapper.Map<EmailSettingsDto>(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email settings");
            throw;
        }
    }
}