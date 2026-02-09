using App.Core.Constants;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class GeneralSeeder : IGeneralSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<GeneralSeeder> _logger;
    private readonly IDateTime _dateTime;
    private readonly string _systemUser = "System";

    public GeneralSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<GeneralSeeder> logger,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedCurrenciesAsync();
            await SeedCountriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding lookup data");
            throw;
        }
    }

    private async Task SeedCurrenciesAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        if (!await _context.Currencies.AnyAsync())
        {
            var currencies = new List<Currency>
            {
                new()
                {
                    Code = "MXN",
                    Name = "Mexican Peso",
                    Symbol = "$",
                    IsActive = true,
                    CreatedBy = _systemUser,
                    CreatedAt = _dateTime.Now
                },
                new()
                {
                    Code = "USD",
                    Name = "US Dollar",
                    Symbol = "$",
                    IsActive = true,
                    CreatedBy = _systemUser,
                    CreatedAt = _dateTime.Now
                },
                new()
                {
                    Code = "CAD",
                    Name = "Canadian Dollar",
                    Symbol = "$",
                    IsActive = true,
                    CreatedBy = _systemUser,
                    CreatedAt = _dateTime.Now
                }
            };

            await _context.Currencies.AddRangeAsync(currencies);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Currencies seeded successfully");
        }
    }

    private async Task SeedCountriesAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        if (!await _context.Countries.AnyAsync())
        {
            var countries = new List<Country>
            {
                new()
                {
                    Code = CountryCodes.Mexico,
                    Name = "Mexico",
                    DefaultCurrencyCode = CurrencyCodes.MexicanPeso,
                    IsActive = true,
                    CreatedBy = _systemUser,
                    CreatedAt = _dateTime.Now
                },
                new()
                {
                    Code = CountryCodes.UnitedStates,
                    Name = "United States",
                    DefaultCurrencyCode = CurrencyCodes.USDollar,
                    IsActive = true,
                    CreatedBy = _systemUser,
                    CreatedAt = _dateTime.Now
                },
                new()
                {
                    Code = CountryCodes.Canada,
                    Name = "Canada",
                    DefaultCurrencyCode = CurrencyCodes.CanadianDollar,
                    IsActive = true,
                    CreatedBy = _systemUser,
                    CreatedAt = _dateTime.Now
                }
            };

            await _context.Countries.AddRangeAsync(countries);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Countries seeded successfully");
        }
    }
}