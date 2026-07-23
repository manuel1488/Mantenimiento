using App.Core.Common;
using App.Core.DTOs.Location;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Location;

public class LocationTicketSettingsService : ILocationTicketSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<LocationTicketSettingsService> _logger;
    private readonly IStringLocalizer<LocationTicketSettingsService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public LocationTicketSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<LocationTicketSettingsService> logger,
        IStringLocalizer<LocationTicketSettingsService> localizer,
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

    public async Task<Result<LocationTicketSettingsDto>> GetByLocationIdAsync(int locationId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.LocationTicketSettings
                .AsNoTracking()
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.LocationId == locationId);

            if (settings == null)
            {
                // Return default/empty settings indicating no specific configuration
                return Result<LocationTicketSettingsDto>.Failure(
                    _localizer["No specific settings for this location. Global settings will be used."]);
            }

            var dto = _mapper.Map<LocationTicketSettingsDto>(settings);
            return Result<LocationTicketSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticket settings for location {LocationId}", locationId);
            return Result<LocationTicketSettingsDto>.Failure(_localizer["Error retrieving ticket settings"]);
        }
    }

    public async Task<Result<LocationTicketSettingsDto>> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.LocationTicketSettings
                .AsNoTracking()
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settings == null)
            {
                return Result<LocationTicketSettingsDto>.Failure(_localizer["Settings not found"]);
            }

            var dto = _mapper.Map<LocationTicketSettingsDto>(settings);
            return Result<LocationTicketSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticket settings {Id}", id);
            return Result<LocationTicketSettingsDto>.Failure(_localizer["Error retrieving ticket settings"]);
        }
    }

    public async Task<Result<LocationTicketSettingsDto>> CreateAsync(CreateLocationTicketSettingsDto createDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Verify location exists
            var locationExists = await context.Locations.AnyAsync(l => l.Id == createDto.LocationId);
            if (!locationExists)
            {
                return Result<LocationTicketSettingsDto>.Failure(_localizer["Location not found"]);
            }

            // Check if settings already exist for this location
            var existingSettings = await context.LocationTicketSettings
                .AnyAsync(s => s.LocationId == createDto.LocationId);

            if (existingSettings)
            {
                return Result<LocationTicketSettingsDto>.Failure(
                    _localizer["Settings already exist for this location"]);
            }

            var entity = _mapper.Map<LocationTicketSettings>(createDto);

            // Set audit fields
            var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";
            var currentTime = _dateTime.Now;
            entity.CreatedBy = currentUser;
            entity.CreatedAt = currentTime;
            entity.ModifiedBy = currentUser;
            entity.ModifiedAt = currentTime;

            context.LocationTicketSettings.Add(entity);
            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<LocationTicketSettingsDto>(entity);
            return Result<LocationTicketSettingsDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket settings for location {LocationId}", createDto.LocationId);
            return Result<LocationTicketSettingsDto>.Failure(_localizer["Error creating ticket settings"]);
        }
    }

    public async Task<Result<LocationTicketSettingsDto>> UpdateAsync(int id, UpdateLocationTicketSettingsDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.LocationTicketSettings.FindAsync(id);
            if (entity == null)
            {
                return Result<LocationTicketSettingsDto>.Failure(_localizer["Settings not found"]);
            }

            // Update properties
            _mapper.Map(updateDto, entity);

            // Update audit fields
            entity.ModifiedBy = await _currentUserService.GetUserIdAsync() ?? "System";
            entity.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<LocationTicketSettingsDto>(entity);
            return Result<LocationTicketSettingsDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket settings {Id}", id);
            return Result<LocationTicketSettingsDto>.Failure(_localizer["Error updating ticket settings"]);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.LocationTicketSettings.FindAsync(id);
            if (entity == null)
            {
                return Result.Failure(_localizer["Settings not found"]);
            }

            context.LocationTicketSettings.Remove(entity);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket settings {Id}", id);
            return Result.Failure(_localizer["Error deleting ticket settings"]);
        }
    }

    public async Task<Result<LocationTicketSettingsDto>> CopyFromGlobalAsync(int locationId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Verify location exists
            var location = await context.Locations.FindAsync(locationId);
            if (location == null)
            {
                return Result<LocationTicketSettingsDto>.Failure(_localizer["Location not found"]);
            }

            // Check if settings already exist
            var existingSettings = await context.LocationTicketSettings
                .AnyAsync(s => s.LocationId == locationId);

            if (existingSettings)
            {
                return Result<LocationTicketSettingsDto>.Failure(
                    _localizer["Settings already exist for this location"]);
            }

            // Get global ticket configuration
            var globalConfig = await context.TicketConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (globalConfig == null)
            {
                return Result<LocationTicketSettingsDto>.Failure(
                    _localizer["Global ticket configuration not found"]);
            }

            // Create location settings from global configuration
            var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";
            var currentTime = _dateTime.Now;

            var entity = new LocationTicketSettings
            {
                LocationId = locationId,
                PrinterName = null, // Location-specific, not copied
                PaperWidth = globalConfig.TicketWidth,
                AutoPrint = false,
                Copies = globalConfig.DefaultCopies,
                HeaderText = globalConfig.CustomHeader,
                FooterText = globalConfig.CustomFooter,
                LogoBase64 = globalConfig.CompanyLogoBase64, // Copy global logo
                ShowLogo = globalConfig.ShowCompanyLogo,
                TaxId = globalConfig.CompanyTaxId,
                LegalName = globalConfig.CompanyName,
                ShowFullAddress = true,
                ShowQrCode = globalConfig.ShowQRCode,
                QrCodeContent = null,
                ShowPrices = true,
                ShowTaxBreakdown = true,
                CreatedBy = currentUser,
                CreatedAt = currentTime,
                ModifiedBy = currentUser,
                ModifiedAt = currentTime
            };

            context.LocationTicketSettings.Add(entity);
            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<LocationTicketSettingsDto>(entity);
            return Result<LocationTicketSettingsDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying global settings to location {LocationId}", locationId);
            return Result<LocationTicketSettingsDto>.Failure(
                _localizer["Error copying global ticket settings"]);
        }
    }

    public async Task<bool> HasSettingsAsync(int locationId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocationTicketSettings
                .AnyAsync(s => s.LocationId == locationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if location {LocationId} has settings", locationId);
            return false;
        }
    }
}
