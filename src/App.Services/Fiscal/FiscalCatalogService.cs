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

    public async Task<(int TotalCount, IList<ClaveProdServSatCatalogoDto> Items)> SearchClavesProdServSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.ClavesProdServSatCatalogo.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x =>
                    x.Codigo.Contains(searchText) ||
                    x.Descripcion.Contains(searchText));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ClaveProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SAT product/service codes catalog");
            throw;
        }
    }

    public async Task<(int TotalCount, IList<ClaveProdServSatCatalogoDto> Items)> SearchClasesProdServSatAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.ClavesProdServSatCatalogo
                .AsNoTracking()
                .Where(x => x.Codigo.EndsWith("00"));

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x =>
                    x.Codigo.Contains(searchText) ||
                    x.Descripcion.Contains(searchText));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ClaveProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SAT product/service class catalog");
            throw;
        }
    }

    public async Task<(int TotalCount, IList<ClaveProdServSatCatalogoDto> Items)> GetProductosPorClaseAsync(
        string claseCodigo,
        string? searchText = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var prefix = claseCodigo.Length >= 6 ? claseCodigo[..6] : claseCodigo;

            var query = context.ClavesProdServSatCatalogo
                .AsNoTracking()
                .Where(x => x.Codigo.StartsWith(prefix) && x.Codigo != claseCodigo);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Descripcion.Contains(searchText));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ClaveProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SAT products for class {ClaseCodigo}", claseCodigo);
            throw;
        }
    }

    public async Task<IList<TipoProdServSatCatalogoDto>> GetTiposProdServSatAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TiposProdServSatCatalogo
                .AsNoTracking()
                .OrderBy(x => x.Codigo)
                .ProjectTo<TipoProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SAT product/service 'Tipo' catalog");
            throw;
        }
    }

    public async Task<IList<SegmentoProdServSatCatalogoDto>> GetSegmentosProdServSatAsync(string tipoCodigo)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.SegmentosProdServSatCatalogo
                .AsNoTracking()
                .Where(x => x.TipoCodigo == tipoCodigo)
                .OrderBy(x => x.Codigo)
                .ProjectTo<SegmentoProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SAT product/service 'Segmento' catalog for tipo {TipoCodigo}", tipoCodigo);
            throw;
        }
    }

    public async Task<IList<FamiliaProdServSatCatalogoDto>> GetFamiliasProdServSatAsync(string segmentoCodigo)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.FamiliasProdServSatCatalogo
                .AsNoTracking()
                .Where(x => x.SegmentoCodigo == segmentoCodigo)
                .OrderBy(x => x.Codigo)
                .ProjectTo<FamiliaProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SAT product/service 'Familia' catalog for segmento {SegmentoCodigo}", segmentoCodigo);
            throw;
        }
    }

    public async Task<IList<ClaveProdServSatCatalogoDto>> GetClasesPorFamiliaAsync(string familiaCodigo)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ClavesProdServSatCatalogo
                .AsNoTracking()
                .Where(x => x.Codigo.StartsWith(familiaCodigo) && x.Codigo.EndsWith("00"))
                .OrderBy(x => x.Codigo)
                .ProjectTo<ClaveProdServSatCatalogoDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SAT product/service classes for familia {FamiliaCodigo}", familiaCodigo);
            throw;
        }
    }
}
