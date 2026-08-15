using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.DTOs.Fiscal;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Fiscal;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Fiscal;

public class FiscalCatalogService : IFiscalCatalogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<FiscalCatalogService> _logger;

    public FiscalCatalogService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<FiscalCatalogService> logger)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IList<RegimenFiscalCatalogoDto>> GetRegimenesFiscalesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.RegimenesFiscalesCatalogo
                .AsNoTracking()
                .OrderBy(x => x.Codigo)
                .ProjectTo<RegimenFiscalCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal regimes catalog");
            throw;
        }
    }

    public async Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.UsosCfdiCatalogo
                .AsNoTracking()
                .OrderBy(x => x.Codigo)
                .ProjectTo<UsoCfdiCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CFDI uses catalog");
            throw;
        }
    }

    public async Task<IList<UsoCfdiCatalogoDto>> GetUsosCfdiPorRegimenAsync(string codigoRegimenFiscal)
    {
        try
        {
            var usos = await GetUsosCfdiAsync();

            return usos
                .Where(u => u.CodigosRegimenFiscal == null ||
                            u.CodigosRegimenFiscal.Split(',').Contains(codigoRegimenFiscal))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CFDI uses for fiscal regime {Codigo}", codigoRegimenFiscal);
            throw;
        }
    }

    public async Task<(int TotalCount, IList<ClaveUnidadSatCatalogoDto> Items)> SearchClavesUnidadSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.ClavesUnidadSatCatalogo.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x =>
                    x.Codigo.Contains(searchText) ||
                    x.Nombre.Contains(searchText));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ClaveUnidadSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SAT unit codes catalog");
            throw;
        }
    }
}
