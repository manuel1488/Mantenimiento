using AutoMapper;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class LookupService : ILookupService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<LookupService> _logger;

    public LookupService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<LookupService> logger)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IList<CountryDto>> GetCountriesAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var countries = await _context.Countries
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<CountryDto>(x))
                .ToListAsync();

            return countries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting countries");
            return new List<CountryDto>();
        }
    }

    public async Task<IList<CurrencyDto>> GetCurrenciesAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var currencies = await _context.Currencies
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<CurrencyDto>(x))
                .ToListAsync();

            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currencies");
            return new List<CurrencyDto>();
        }
    }

    public async Task<CurrencyDto?> GetCountryCurrencyAsync(string countryCode)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == countryCode && x.IsActive);

            if (country == null)
                return null;

            var currency = await _context.Currencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == country.DefaultCurrencyCode && x.IsActive);

            return currency != null ? _mapper.Map<CurrencyDto>(currency) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currency for country {CountryCode}", countryCode);
            return null;
        }
    }
}