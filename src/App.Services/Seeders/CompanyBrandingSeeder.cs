using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Services.Seeders;

public class CompanyBrandingSeeder : ICompanyBrandingSeeder
{
    private const string SystemUser = "System";
    private const string DefaultTimeZoneId = "America/Mexico_City";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ApplicationOptions _applicationOptions;
    private readonly ILogger<CompanyBrandingSeeder> _logger;

    public CompanyBrandingSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IOptions<ApplicationOptions> applicationOptions,
        ILogger<CompanyBrandingSeeder> logger)
    {
        _contextFactory = contextFactory;
        _applicationOptions = applicationOptions.Value;
        _logger = logger;
    }

    // Ensures CompanySettings.CompanyName is always populated from the deployment's brand
    // profile (Branding/{profile}.json via ApplicationOptions.Name), so every document/report
    // has a single source of truth (the DB) with no runtime fallback to config. Only touches
    // CompanyName — Initial Setup owns the rest (country/currency/timezone) and this seeder
    // must not overwrite whatever the admin has already configured there.
    public async Task SeedAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.CompanySettings
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;

            if (settings == null)
            {
                context.CompanySettings.Add(new CompanySettings
                {
                    CompanyName = _applicationOptions.Name,
                    CountryCode = "MX",
                    CurrencyCode = "MXN",
                    TimeZoneId = DefaultTimeZoneId,
                    TimeZoneDisplayName = ResolveTimeZoneDisplayName(DefaultTimeZoneId),
                    CreatedBy = SystemUser,
                    CreatedAt = now,
                    ModifiedBy = SystemUser,
                    ModifiedAt = now
                });

                await context.SaveChangesAsync();
                _logger.LogInformation(
                    "Company settings seeded with brand name '{CompanyName}'", _applicationOptions.Name);
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.CompanyName))
            {
                settings.CompanyName = _applicationOptions.Name;
                settings.ModifiedBy = SystemUser;
                settings.ModifiedAt = now;
                await context.SaveChangesAsync();
                _logger.LogInformation(
                    "Company name backfilled from brand profile: '{CompanyName}'", _applicationOptions.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding company branding");
            throw;
        }
    }

    private static string? ResolveTimeZoneDisplayName(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId).DisplayName;
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }
}
