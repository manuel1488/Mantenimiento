using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Fiscal;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class FiscalCatalogSeeder : IFiscalCatalogSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFiscalCatalogDataReader _dataReader;
    private readonly ILogger<FiscalCatalogSeeder> _logger;
    private readonly IDateTime _dateTimeService;
    private const string SystemUser = "System";

    public FiscalCatalogSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFiscalCatalogDataReader dataReader,
        ILogger<FiscalCatalogSeeder> logger,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _dataReader = dataReader;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedRegimenesFiscalesAsync();
            await SeedUsosCfdiAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding fiscal catalogs");
            throw;
        }
    }

    private async Task SeedRegimenesFiscalesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<RegimenFiscalCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetRegimenesFiscalesAsync();
        var now = _dateTimeService.Now;

        var entities = dtos.Select(dto => new RegimenFiscalCatalogo
        {
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            CreatedBy = SystemUser,
            CreatedAt = now
        });

        await context.Set<RegimenFiscalCatalogo>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        _logger.LogInformation("Fiscal regimes catalog seeded successfully");
    }

    private async Task SeedUsosCfdiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<UsoCfdiCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetUsosCfdiAsync();
        var now = _dateTimeService.Now;

        var entities = dtos.Select(dto => new UsoCfdiCatalogo
        {
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            CodigosRegimenFiscal = dto.CodigosRegimenFiscal,
            CreatedBy = SystemUser,
            CreatedAt = now
        });

        await context.Set<UsoCfdiCatalogo>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        _logger.LogInformation("CFDI uses catalog seeded successfully");
    }
}
