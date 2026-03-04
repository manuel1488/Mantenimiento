using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Interfaces.Billing;
using App.Models.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Billing;

public class CfdiPostalCodeService : ICfdiPostalCodeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CfdiPostalCodeService> _logger;

    public CfdiPostalCodeService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CfdiPostalCodeService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Result<CfdiPostalCodeDto>> GetByCodeAsync(string postalCode)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var record = await context.CfdiPostalCodes
                .AsNoTracking()
                .Where(x => x.Code == postalCode)
                .Select(x => new CfdiPostalCodeDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    StateId = x.StateId,
                    MunicipalityId = x.MunicipalityId,
                    LocalityId = x.LocalityId,
                    IsBorderZone = x.IsBorderZone,
                    TimeZoneName = x.TimeZoneName,
                    IanaTimeZoneId = x.IanaTimeZoneId,
                    OffsetWinter = x.OffsetWinter,
                    OffsetSummer = x.OffsetSummer
                })
                .FirstOrDefaultAsync();

            if (record == null)
                return Result<CfdiPostalCodeDto>.Failure($"Postal code {postalCode} not found in CFDI catalog");

            return Result<CfdiPostalCodeDto>.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up postal code {PostalCode}", postalCode);
            return Result<CfdiPostalCodeDto>.Failure("Error looking up postal code timezone");
        }
    }

    public async Task<bool> ExistsAsync(string postalCode)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CfdiPostalCodes
                .AsNoTracking()
                .AnyAsync(x => x.Code == postalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking postal code existence {PostalCode}", postalCode);
            return false;
        }
    }
}
