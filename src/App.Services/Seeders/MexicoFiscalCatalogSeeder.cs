using App.Core.DTOs.Billing.Mexico;
using App.Core.Interfaces;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class MexicoFiscalCatalogSeeder : IMexicoFiscalSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFiscalCatalogDataReader _dataReader;
    private readonly ILogger<MexicoFiscalCatalogSeeder> _logger;
    private readonly IDateTime _dateTime;
    private readonly string _systemUser = "System";
    private const int BatchSize = 1000;

    public MexicoFiscalCatalogSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFiscalCatalogDataReader dataReader,
        ILogger<MexicoFiscalCatalogSeeder> logger,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _dataReader = dataReader;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedFiscalRegimesAsync();
            await SeedPaymentFormsAsync();
            await SeedPaymentMethodsAsync();
            await SeedCfdiUsesAsync();
            await SeedProductServicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Mexico fiscal catalogs");
            throw;
        }
    }

    private async Task BulkInsertAsync<TEntity, TDto>(
        IEnumerable<TDto> dtos,
        Func<TDto, TEntity> mapEntity,
        string catalogName) 
        where TEntity : class
    {
        if (!dtos.Any())
        {
            _logger.LogWarning("No records to insert for {CatalogName}", catalogName);
            return;
        }

        var totalRecords = dtos.Count();
        var batches = (totalRecords + BatchSize - 1) / BatchSize;
        var now = _dateTime.Now;

        _logger.LogInformation("Starting bulk insert for {CatalogName}. Total records: {TotalRecords}", 
            catalogName, totalRecords);

        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var dbSet = _context.Set<TEntity>();

            using var transaction = await _context.Database.BeginTransactionAsync();

            for (int i = 0; i < batches; i++)
            {
                var batch = dtos.Skip(i * BatchSize).Take(BatchSize);
                var entities = batch.Select(dto =>
                {
                    var entity = mapEntity(dto);
                    SetAuditFields(entity, now);
                    return entity;
                });

                await dbSet.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Processed batch {CurrentBatch}/{TotalBatches} for {CatalogName}", 
                    i + 1, batches, catalogName);
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Successfully seeded {CatalogName}. Total records: {TotalRecords}", 
                catalogName, totalRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk inserting {CatalogName}", catalogName);
            throw;
        }
    }

    private void SetAuditFields<T>(T entity, DateTime timestamp)
    {
        if (entity is null) return;

        var type = entity.GetType();
        type.GetProperty("CreatedBy")?.SetValue(entity, _systemUser);
        type.GetProperty("CreatedAt")?.SetValue(entity, timestamp);
    }

    private async Task SeedFiscalRegimesAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        if (!await _context.MexicoFiscalRegimes.AsNoTracking().AnyAsync())
        {
            var dtos = await _dataReader.GetFiscalRegimesAsync();
            await BulkInsertAsync(                
                dtos,
                dto => new MexicoFiscalRegime
                {
                    Code = dto.Code,
                    Description = dto.Description
                },
                "Fiscal Regimes");
        }
    }

    private async Task SeedPaymentFormsAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        if (!await _context.MexicoPaymentForms.AsNoTracking().AnyAsync())
        {
            var dtos = await _dataReader.GetPaymentFormsAsync();
            await BulkInsertAsync(
                dtos,
                dto => new MexicoPaymentForm
                {
                    Code = dto.Code,
                    Description = dto.Description
                },
                "Payment Forms");
        }
    }

    private async Task SeedPaymentMethodsAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        if (!await _context.MexicoPaymentMethods.AsNoTracking().AnyAsync())
        {
            var dtos = await _dataReader.GetPaymentMethodsAsync();
            await BulkInsertAsync(
                dtos,
                dto => new MexicoPaymentMethod
                {
                    Code = dto.Code,
                    Description = dto.Description
                },
                "Payment Methods");
        }
    }

    private async Task SeedCfdiUsesAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var dtos = await _dataReader.GetCfdiUsesAsync();

        if (!await _context.MexicoCfdiUses.AsNoTracking().AnyAsync())
        {
            await BulkInsertAsync(
                dtos,
                dto => new MexicoCfdiUse
                {
                    Code = dto.Code,
                    Description = dto.Description,
                    FiscalRegimeCodes = dto.FiscalRegimeCodes
                },
                "CFDI Uses");
        }
        else
        {
            // Update FiscalRegimeCodes for existing records that are missing it
            var dtosMap = dtos.ToDictionary(d => d.Code);
            var existing = await _context.MexicoCfdiUses
                .Where(x => x.FiscalRegimeCodes == null)
                .ToListAsync();

            var now = _dateTime.Now;
            foreach (var item in existing)
            {
                if (dtosMap.TryGetValue(item.Code, out var dto))
                {
                    item.FiscalRegimeCodes = dto.FiscalRegimeCodes;
                    item.ModifiedBy = _systemUser;
                    item.ModifiedAt = now;
                }
            }

            if (existing.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated FiscalRegimeCodes for {Count} CFDI Uses", existing.Count);
            }
        }
    }

    private async Task SeedProductServicesAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        if (!await _context.MexicoProductServices.AsNoTracking().AnyAsync())
        {
            var dtos = await _dataReader.GetProductServicesAsync();
            await BulkInsertAsync(
                dtos,
                dto => new MexicoProductService
                {
                    Code = dto.Code,
                    Description = dto.Description
                },
                "Product Services");
        }
    }
}