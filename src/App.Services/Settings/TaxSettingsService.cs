using AutoMapper;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Core.Validators;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class TaxSettingsService : ITaxSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<TaxSettingsService> _logger;
    private readonly IStringLocalizer<TaxSettingsService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly TaxIdValidator _taxIdValidator;

    public TaxSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<TaxSettingsService> logger,
        IStringLocalizer<TaxSettingsService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        TaxIdValidator taxIdValidator)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _taxIdValidator = taxIdValidator;
    }

    public async Task<TaxSettingsDto?> GetSettingsAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var settings = await _context.TaxSettings
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return settings != null ? _mapper.Map<TaxSettingsDto>(settings) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax settings");
            throw;
        }
    }

    public async Task<TaxSettingsDto> UpdateSettingsAsync(UpdateTaxSettingsDto updateDto)
    {
        try
        {
            // Validate settings before update
            var validationErrors = await ValidateSettingsAsync(updateDto.CountryCode, updateDto);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(
                    string.Join(Environment.NewLine, validationErrors));
            }

            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Get current settings or create new ones if they don't exist
            var settings = await _context.TaxSettings
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new TaxSettings
                {
                    CreatedBy = _currentUserService.FullName ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                _context.TaxSettings.Add(settings);
            }

            // Update properties
            _mapper.Map(updateDto, settings);

            // Populate postal code timezone from CFDI catalog (MX only)
            if (string.Equals(settings.CountryCode, "MX", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(settings.PostalCode))
            {
                var postalCodeRecord = await _context.CfdiPostalCodes
                    .AsNoTracking()
                    .Where(x => x.Code == settings.PostalCode)
                    .Select(x => new { x.TimeZoneName, x.IanaTimeZoneId, x.OffsetWinter, x.OffsetSummer })
                    .FirstOrDefaultAsync();

                if (postalCodeRecord != null)
                {
                    settings.PostalCodeTimeZoneName = postalCodeRecord.TimeZoneName;
                    settings.PostalCodeIanaTimeZoneId = postalCodeRecord.IanaTimeZoneId;
                    settings.PostalCodeOffsetWinter = postalCodeRecord.OffsetWinter;
                    settings.PostalCodeOffsetSummer = postalCodeRecord.OffsetSummer;
                }
                else
                {
                    _logger.LogWarning("Postal code {PostalCode} not found in CFDI catalog — timezone fields cleared", settings.PostalCode);
                    settings.PostalCodeTimeZoneName = null;
                    settings.PostalCodeIanaTimeZoneId = null;
                    settings.PostalCodeOffsetWinter = null;
                    settings.PostalCodeOffsetSummer = null;
                }
            }
            else if (!string.Equals(settings.CountryCode, "MX", StringComparison.OrdinalIgnoreCase))
            {
                settings.PostalCodeTimeZoneName = null;
                settings.PostalCodeIanaTimeZoneId = null;
                settings.PostalCodeOffsetWinter = null;
                settings.PostalCodeOffsetSummer = null;
            }

            // Update audit fields
            settings.ModifiedBy = _currentUserService.FullName ?? "Unknown";
            settings.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            return _mapper.Map<TaxSettingsDto>(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax settings");
            throw;
        }
    }

    public async Task<IList<string>> ValidateSettingsAsync(string countryCode, UpdateTaxSettingsDto settings)
    {
        var errors = new List<string>();
        await using var _context = await _contextFactory.CreateDbContextAsync();

        // Validate country exists
        var country = await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == countryCode && x.IsActive);

        if (country == null)
        {
            errors.Add(_localizer["Country_Invalid"]);
            return errors;
        }

        // Validate Tax ID
        var taxIdValidation = _taxIdValidator.ValidateTaxId(countryCode, settings.TaxId);
        if (!taxIdValidation.IsSuccess)
        {
            errors.Add(taxIdValidation.Error!);
        }

        // Validate country-specific fields
        switch (countryCode.ToUpper())
        {
            case "MX":
                if (string.IsNullOrEmpty(settings.MxDefaultCfdiUse))
                    errors.Add(_localizer["Mexico_CfdiRequired"]);
                
                if (string.IsNullOrEmpty(settings.MxDefaultPaymentMethod))
                    errors.Add(_localizer["Mexico_PaymentMethodRequired"]);
                
                if (string.IsNullOrEmpty(settings.MxDefaultPaymentType))
                    errors.Add(_localizer["Mexico_PaymentTypeRequired"]);
                break;

            case "CA":
                if (string.IsNullOrEmpty(settings.CaGstNumber))
                    errors.Add(_localizer["Canada_GstRequired"]);
                break;
        }

        return errors;
    }
}