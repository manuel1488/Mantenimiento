using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.Common;
using App.Core.DTOs.Obras;
using App.Core.Enums.Cotizaciones;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Models.Cotizaciones;
using App.Models.Data.Contexts;
using App.Models.Obras;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Obras;

public class ObraService : IObraService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ObraService> _logger;
    private readonly IStringLocalizer<ObraService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;
    private readonly IFileStorageService _fileStorageService;

    public ObraService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ObraService> logger,
        IStringLocalizer<ObraService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        IFileStorageService fileStorageService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<List<ObraDto>>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var obras = await context.Obras
                .AsNoTracking()
                .Include(o => o.Cliente)
                .OrderByDescending(o => o.FechaSolicitud)
                .ProjectTo<ObraDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<ObraDto>>.Success(obras);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving obras");
            return Result<List<ObraDto>>.Failure(_localizer["Error retrieving obras"]);
        }
    }

    public async Task<Result<ObraDto>> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var obra = await context.Obras
                .AsNoTracking()
                .Include(o => o.Cliente)
                .Include(o => o.Actividades).ThenInclude(a => a.Servicio).ThenInclude(s => s.UnidadMedida)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (obra == null)
                return Result<ObraDto>.Failure(_localizer["Obra not found"]);

            return Result<ObraDto>.Success(_mapper.Map<ObraDto>(obra));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving obra {Id}", id);
            return Result<ObraDto>.Failure(_localizer["Error retrieving obra"]);
        }
    }

    public async Task<Result<ObraDto>> CreateAsync(CreateObraDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var entity = _mapper.Map<Obra>(dto);
                    entity.Estado = ObraEstado.Solicitada;

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.FechaSolicitud = currentTime;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.Obras.Add(entity);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var created = await context.Obras
                        .AsNoTracking()
                        .Include(o => o.Cliente)
                        .FirstAsync(o => o.Id == entity.Id);

                    return Result<ObraDto>.Success(_mapper.Map<ObraDto>(created));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating obra");
            return Result<ObraDto>.Failure(_localizer["Error creating obra"]);
        }
    }

    public async Task<Result<ObraDto>> UpdateAsync(UpdateObraDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var entity = await context.Obras.FindAsync(dto.Id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Obra not found"]);
                    }

                    if (entity.Estado is ObraEstado.Finalizada or ObraEstado.Facturada)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Cannot modify an Obra that is Finalizada or Facturada"]);
                    }

                    _mapper.Map(dto, entity);

                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Obras
                        .AsNoTracking()
                        .Include(o => o.Cliente)
                        .FirstAsync(o => o.Id == entity.Id);

                    return Result<ObraDto>.Success(_mapper.Map<ObraDto>(updated));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating obra {Id}", dto.Id);
            return Result<ObraDto>.Failure(_localizer["Error updating obra"]);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var entity = await context.Obras.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Obra not found"]);
                    }

                    if (await context.Actividades.AnyAsync(a => a.ObraId == id))
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Cannot delete an Obra with existing Actividades"]);
                    }

                    if (await context.Facturas.AnyAsync(f => f.ObraId == id))
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Cannot delete an Obra with an existing Factura"]);
                    }

                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();

                    context.Obras.Remove(entity);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting obra {Id}", id);
            return Result.Failure(_localizer["Error deleting obra"]);
        }
    }

    public async Task<Result<ObraDto>> FinalizarAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var entity = await context.Obras.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Obra not found"]);
                    }

                    if (entity.Estado is not (ObraEstado.Aprobada or ObraEstado.EnProceso))
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Only an Obra in Aprobada or EnProceso can be Finalizada"]);
                    }

                    entity.Estado = ObraEstado.Finalizada;
                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Obras
                        .AsNoTracking()
                        .Include(o => o.Cliente)
                        .FirstAsync(o => o.Id == entity.Id);

                    return Result<ObraDto>.Success(_mapper.Map<ObraDto>(updated));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing obra {Id}", id);
            return Result<ObraDto>.Failure(_localizer["Error finalizing obra"]);
        }
    }

    public async Task<Result<ObraDto>> CreateFromCotizacionAsync(int cotizacionId, ConvertirCotizacionAObraDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var cotizacion = await context.Cotizaciones
                        .Include(c => c.Lineas)
                        .Include(c => c.Fotos)
                        .FirstOrDefaultAsync(c => c.Id == cotizacionId);

                    if (cotizacion == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Cotizacion not found"]);
                    }

                    if (cotizacion.Estado != CotizacionEstado.Aprobada)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Only an Aprobada Cotizacion can be converted to an Obra"]);
                    }

                    var yaConvertida = await context.Obras.AnyAsync(o => o.CotizacionOrigenId == cotizacionId);
                    if (yaConvertida)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["This Cotizacion was already converted to an Obra"]);
                    }

                    // The Cotización's líneas already snapshot the Servicio name/unidad/precio at
                    // quoting time (money figures below come from that snapshot, never from the live
                    // catalog). The catalog Servicio may since have changed or been soft-deleted, so
                    // its current rendimiento is looked up tolerantly here — it only feeds a
                    // best-effort TiempoEstimadoDias, never blocks the conversion.
                    var servicioIds = cotizacion.Lineas.Select(l => l.ServicioId).Distinct().ToList();
                    var rendimientos = await context.Servicios
                        .IgnoreQueryFilters()
                        .Where(s => servicioIds.Contains(s.Id))
                        .ToDictionaryAsync(s => s.Id, s => s.RendimientoDiasPorUnidad);

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;

                    var obra = new Obra
                    {
                        ClienteId = cotizacion.ClienteId,
                        Direccion = dto.Direccion,
                        Urgente = dto.Urgente,
                        Estado = ObraEstado.Aprobada,
                        FechaSolicitud = currentTime,
                        CotizacionOrigenId = cotizacion.Id,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    };

                    foreach (var linea in cotizacion.Lineas)
                    {
                        var rendimiento = rendimientos.GetValueOrDefault(linea.ServicioId);

                        obra.Actividades.Add(new Actividad
                        {
                            ServicioId = linea.ServicioId,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.PrecioUnitario,
                            Costo = linea.Subtotal,
                            RendimientoDiasPorUnidad = rendimiento,
                            TiempoEstimadoDias = linea.Cantidad * rendimiento,
                            Estado = ActividadEstado.Pendiente,
                            CreatedBy = currentUser,
                            CreatedAt = currentTime,
                            ModifiedBy = currentUser,
                            ModifiedAt = currentTime
                        });
                    }

                    context.Obras.Add(obra);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var created = await context.Obras
                        .AsNoTracking()
                        .Include(o => o.Cliente)
                        .FirstAsync(o => o.Id == obra.Id);

                    // The Cotización's fotos remain editable/deletable after conversion (they're not
                    // locked to Aprobada), so they're copied here into each Actividad's evidencia
                    // "Antes" as an independent snapshot — later changes to the Cotización's fotos
                    // must not retroactively alter the Obra's evidence trail. Best-effort: a copy
                    // failure is logged and skipped rather than failing the whole conversion, since
                    // the Obra/Actividades are already committed at this point.
                    if (cotizacion.Fotos.Count > 0)
                        await CopiarFotosACtividadesAsync(cotizacion.Fotos, obra.Actividades, currentUser, currentTime);

                    return Result<ObraDto>.Success(_mapper.Map<ObraDto>(created));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting cotizacion {Id} to obra", cotizacionId);
            return Result<ObraDto>.Failure(_localizer["Error converting cotizacion to obra"]);
        }
    }

    private async Task CopiarFotosACtividadesAsync(
        IEnumerable<CotizacionFoto> fotos, IEnumerable<Actividad> actividades, string currentUser, DateTime currentTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var actividad in actividades)
        {
            var keyPrefix = $"evidencias/actividad-{actividad.Id}";

            foreach (var foto in fotos)
            {
                try
                {
                    var copyResult = await _fileStorageService.CopyAsync(foto.FileKey, keyPrefix, ExtensionFromKey(foto.FileKey));
                    if (!copyResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Failed to copy cotizacion foto {FileKey} to actividad {ActividadId} evidencia: {Error}",
                            foto.FileKey, actividad.Id, copyResult.Error);
                        continue;
                    }

                    string? thumbnailKey = null;
                    if (foto.ThumbnailFileKey is not null)
                    {
                        var thumbnailCopyResult = await _fileStorageService.CopyAsync(
                            foto.ThumbnailFileKey, $"{keyPrefix}/thumb", ExtensionFromKey(foto.ThumbnailFileKey));
                        thumbnailKey = thumbnailCopyResult.IsSuccess ? thumbnailCopyResult.Value : null;
                    }

                    context.ActividadEvidenciaFotos.Add(new ActividadEvidenciaFoto
                    {
                        ActividadId = actividad.Id,
                        Tipo = TipoEvidencia.Antes,
                        RutaArchivo = copyResult.Value!,
                        RutaArchivoThumbnail = thumbnailKey,
                        FechaCarga = foto.FechaCarga,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error copying cotizacion foto {FileKey} to actividad {ActividadId} evidencia", foto.FileKey, actividad.Id);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static string ExtensionFromKey(string key)
    {
        var extension = Path.GetExtension(key).TrimStart('.');
        return string.IsNullOrEmpty(extension) ? "jpg" : extension;
    }
}
