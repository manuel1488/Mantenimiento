using App.Core.DTOs.Settings;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class TaxRateService : ITaxRateService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<TaxRateService> _logger;
    private readonly IStringLocalizer<TaxRateService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public TaxRateService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<TaxRateService> logger,
        IStringLocalizer<TaxRateService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<TaxRateDto?> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var rate = await context.TaxRates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return rate != null ? _mapper.Map<TaxRateDto>(rate) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax rate by id {Id}", id);
            throw;
        }
    }

    public async Task<IList<TaxRateDto>> GetActiveRatesAsync(string countryCode, string? provinceCode = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.TaxRates
                .AsNoTracking()
                .Where(x => x.CountryCode == countryCode &&
                           x.IsActive &&
                           x.EffectiveFrom <= _dateTime.Now &&
                           (!x.EffectiveTo.HasValue || x.EffectiveTo > _dateTime.Now));

            if (!string.IsNullOrEmpty(provinceCode))
            {
                query = query.Where(x => x.ProvinceCode == provinceCode);
            }

            var rates = await query
                .OrderBy(x => x.Code)
                .Select(x => _mapper.Map<TaxRateDto>(x))
                .ToListAsync();

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tax rates for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<IList<TaxRateDto>> GetHistoricalRatesAsync(
        string countryCode,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var rates = await context.TaxRates
                .AsNoTracking()
                .Where(x => x.CountryCode == countryCode &&
                           x.EffectiveFrom <= endDate &&
                           (!x.EffectiveTo.HasValue || x.EffectiveTo >= startDate))
                .OrderByDescending(x => x.EffectiveFrom)
                .Select(x => _mapper.Map<TaxRateDto>(x))
                .ToListAsync();

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical tax rates for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<TaxRateDto> CreateRateAsync(CreateTaxRateDto createDto)
    {
        try
        {
            // Validate rate
            if (!await ValidateRateAsync(createDto.CountryCode, createDto.Rate))
            {
                throw new InvalidOperationException(
                    L["Invalid tax rate value. Must be between 0 and 100"]);
            }

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Validate unique code for country
            var codeExists = await context.TaxRates
                .AnyAsync(x => x.CountryCode == createDto.CountryCode &&
                              x.Code == createDto.Code &&
                              x.IsActive);

            if (codeExists)
            {
                throw new InvalidOperationException(
                    L["Tax rate code already exists for this country"]);
            }

            // If default rate, unmark other default rates
            if (createDto.IsDefault)
            {
                var existingDefaults = await context.TaxRates
                    .Where(x => x.CountryCode == createDto.CountryCode &&
                           x.IsDefault &&
                           x.IsActive)
                    .ToListAsync();

                foreach (var rate in existingDefaults)
                {
                    rate.IsDefault = false;
                    rate.ModifiedBy = _currentUserService.FullName ?? "Unknown";
                    rate.ModifiedAt = _dateTime.Now;
                }
            }

            var entity = _mapper.Map<TaxRate>(createDto);
            entity.CreatedBy = _currentUserService.FullName ?? "Unknown";
            entity.CreatedAt = _dateTime.Now;

            context.TaxRates.Add(entity);
            await context.SaveChangesAsync();

            return _mapper.Map<TaxRateDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax rate");
            throw;
        }
    }

    public async Task<TaxRateDto> UpdateRateAsync(int id, UpdateTaxRateDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var rate = await context.TaxRates.FindAsync(id);
            if (rate == null)
            {
                throw new InvalidOperationException(L["Tax rate not found"]);
            }

            // Validte rate if it changed
            if (updateDto.Rate != rate.Rate &&
                !await ValidateRateAsync(rate.CountryCode, updateDto.Rate))
            {
                throw new InvalidOperationException(
                    L["Invalid tax rate value. Must be between 0 and 100"]);
            }

            // Manage changes in default rate
            if (updateDto.IsDefault && !rate.IsDefault)
            {
                var existingDefaults = await context.TaxRates
                    .Where(x => x.CountryCode == rate.CountryCode &&
                           x.Id != id &&
                           x.IsDefault &&
                           x.IsActive)
                    .ToListAsync();

                foreach (var defaultRate in existingDefaults)
                {
                    defaultRate.IsDefault = false;
                    defaultRate.ModifiedBy = _currentUserService.FullName ?? "Unknown";
                    defaultRate.ModifiedAt = _dateTime.Now;
                }
            }

            // Validar fechas efectivas
            if (updateDto.EffectiveTo.HasValue && updateDto.EffectiveTo.Value <= rate.EffectiveFrom)
            {
                throw new InvalidOperationException(
                    L["Effective end date must be after effective start date"]);
            }

            _mapper.Map(updateDto, rate);
            rate.ModifiedBy = _currentUserService.FullName ?? "Unknown";
            rate.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return _mapper.Map<TaxRateDto>(rate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax rate {Id}", id);
            throw;
        }
    }

    public async Task<decimal> GetEffectiveRateAsync(
        string countryCode,
        string? provinceCode = null,
        DateTime? effectiveDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var date = effectiveDate ?? _dateTime.Now;

            var rate = await context.TaxRates
                .AsNoTracking()
                .Where(x => x.CountryCode == countryCode &&
                           x.IsActive &&
                           x.EffectiveFrom <= date &&
                           (!x.EffectiveTo.HasValue || x.EffectiveTo > date) &&
                           (string.IsNullOrEmpty(provinceCode) || x.ProvinceCode == provinceCode))
                .OrderByDescending(x => x.IsDefault)
                .FirstOrDefaultAsync();

            return rate?.Rate ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective tax rate for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<bool> DeleteRateAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var rate = await context.TaxRates.FindAsync(id);
                if (rate == null) return false;

                rate.IsActive = false;
                rate.IsDefault = false;
                rate.ModifiedBy = _currentUserService.FullName;
                await context.SaveChangesAsync();

                rate.DeletedBy = _currentUserService.FullName;
                context.TaxRates.Remove(rate);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tax rate {Id}", id);
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> ValidateRateAsync(string countryCode, decimal rate)
    {
        // Basic validation: rate must be between 0 and 100
        if (rate < 0 || rate > 100)
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Validate that the country exists
        var countryExists = await context.Countries
            .AnyAsync(x => x.Code == countryCode && x.IsActive);

        return countryExists;
    }
}