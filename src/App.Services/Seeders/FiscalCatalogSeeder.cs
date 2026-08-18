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
            await SeedClavesUnidadSatAsync();
            await SeedClavesProdServSatAsync();
            await SeedTiposProdServSatAsync();
            await SeedSegmentosProdServSatAsync();
            await SeedFamiliasProdServSatAsync();
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

    private async Task SeedClavesUnidadSatAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<ClaveUnidadSatCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetClavesUnidadSatAsync();
        var now = _dateTimeService.Now;

        var entities = dtos.Select(dto => new ClaveUnidadSatCatalogo
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Simbolo = dto.Simbolo,
            CreatedBy = SystemUser,
            CreatedAt = now
        });

        await context.Set<ClaveUnidadSatCatalogo>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        _logger.LogInformation("SAT unit codes catalog seeded successfully");
    }

    private async Task SeedClavesProdServSatAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<ClaveProdServSatCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetClavesProdServSatAsync();
        var now = _dateTimeService.Now;

        const int batchSize = 5000;
        foreach (var batch in dtos.Chunk(batchSize))
        {
            var entities = batch.Select(dto => new ClaveProdServSatCatalogo
            {
                Codigo = dto.Codigo,
                Descripcion = dto.Descripcion,
                CreatedBy = SystemUser,
                CreatedAt = now
            });

            await context.Set<ClaveProdServSatCatalogo>().AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }

        _logger.LogInformation("SAT product/service codes catalog seeded successfully");
    }

    private async Task SeedTiposProdServSatAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<TipoProdServSatCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetTiposProdServSatAsync();
        var now = _dateTimeService.Now;

        var entities = dtos.Select(dto => new TipoProdServSatCatalogo
        {
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            CreatedBy = SystemUser,
            CreatedAt = now
        });

        await context.Set<TipoProdServSatCatalogo>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        _logger.LogInformation("SAT product/service 'Tipo' catalog seeded successfully");
    }

    private async Task SeedSegmentosProdServSatAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<SegmentoProdServSatCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetSegmentosProdServSatAsync();
        var now = _dateTimeService.Now;

        var entities = dtos.Select(dto => new SegmentoProdServSatCatalogo
        {
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            TipoCodigo = dto.TipoCodigo,
            CreatedBy = SystemUser,
            CreatedAt = now
        });

        await context.Set<SegmentoProdServSatCatalogo>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        _logger.LogInformation("SAT product/service 'Segmento' catalog seeded successfully");
    }

    private async Task SeedFamiliasProdServSatAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Set<FamiliaProdServSatCatalogo>().AsNoTracking().AnyAsync())
            return;

        var dtos = await _dataReader.GetFamiliasProdServSatAsync();
        var now = _dateTimeService.Now;

        var entities = dtos.Select(dto => new FamiliaProdServSatCatalogo
        {
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            SegmentoCodigo = dto.SegmentoCodigo,
            CreatedBy = SystemUser,
            CreatedAt = now
        });

        await context.Set<FamiliaProdServSatCatalogo>().AddRangeAsync(entities);
        await context.SaveChangesAsync();

        _logger.LogInformation("SAT product/service 'Familia' catalog seeded successfully");
    }
}
