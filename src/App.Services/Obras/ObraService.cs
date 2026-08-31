using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.Common;
using App.Core.DTOs.Obras;
using App.Core.Enums.Cotizaciones;
using App.Core.Enums.Notifications;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Core.Models.Notifications;
using App.Models.Clientes;
using App.Models.Cotizaciones;
using App.Models.Data.Contexts;
using App.Models.Obras;
using App.Services.Notifications;
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
    private readonly IImageService _imageService;
    private readonly INotificationService _notificationService;

    public ObraService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ObraService> logger,
        IStringLocalizer<ObraService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        IFileStorageService fileStorageService,
        IImageService imageService,
        INotificationService notificationService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _fileStorageService = fileStorageService;
        _imageService = imageService;
        _notificationService = notificationService;
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
                        .Include(o => o.Actividades)
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
                        .Include(o => o.Actividades)
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

    public async Task<Result<ObraDto>> IniciarAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
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

                    if (entity.Estado != ObraEstado.Aprobada)
                    {
                        await transaction.RollbackAsync();
                        return Result<ObraDto>.Failure(_localizer["Only an Obra in Aprobada can be started"]);
                    }

                    entity.Estado = ObraEstado.EnProceso;
                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Obras
                        .AsNoTracking()
                        .Include(o => o.Cliente)
                        .Include(o => o.Actividades)
                        .FirstAsync(o => o.Id == entity.Id);

                    return Result<ObraDto>.Success(_mapper.Map<ObraDto>(updated));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            if (result.IsSuccess)
                await NotifyObraIniciadaAsync(id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting obra {Id}", id);
            return Result<ObraDto>.Failure(_localizer["Error starting obra"]);
        }
    }

    private async Task NotifyObraIniciadaAsync(int obraId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var obra = await context.Obras
                .AsNoTracking()
                .Include(o => o.Cliente)
                .FirstOrDefaultAsync(o => o.Id == obraId);

            if (obra?.Cliente.Correo is not { Length: > 0 } correo)
                return;

            var message = new NotificationMessage
            {
                EventType = "ObraIniciada",
                RelatedEntityType = nameof(Obra),
                RelatedEntityId = obraId,
                Subject = _localizer["Your project has started"],
                Body = _localizer["Work has started on your project at {0}.", obra.Direccion],
                Recipients = new Dictionary<NotificationChannelType, string>
                {
                    [NotificationChannelType.Email] = correo
                }
            };

            await _notificationService.NotifyAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying client that obra {Id} started", obraId);
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
                            .ThenInclude(l => l.Fotos)
                        .Include(c => c.FotosGenerales)
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

                    // Track which Actividad was generated from which línea so each línea's fotos can
                    // later be copied only into its own Actividad (1:1 by Servicio), not into every
                    // Actividad of the Obra.
                    var lineaActividadPairs = new List<(CotizacionLinea Linea, Actividad Actividad)>();

                    foreach (var linea in cotizacion.Lineas)
                    {
                        var actividad = new Actividad
                        {
                            ServicioId = linea.ServicioId,
                            Descripcion = linea.Descripcion,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.PrecioUnitario,
                            Costo = linea.Subtotal,
                            RendimientoDiasPorUnidad = linea.RendimientoDiasPorUnidad,
                            TiempoEstimadoDias = linea.TiempoEstimadoDias,
                            Estado = ActividadEstado.Pendiente,
                            CreatedBy = currentUser,
                            CreatedAt = currentTime,
                            ModifiedBy = currentUser,
                            ModifiedAt = currentTime
                        };

                        obra.Actividades.Add(actividad);
                        lineaActividadPairs.Add((linea, actividad));
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
                    var pairsConFotos = lineaActividadPairs.Where(p => p.Linea.Fotos.Count > 0).ToList();
                    if (pairsConFotos.Count > 0)
                        await CopiarFotosACtividadesAsync(pairsConFotos, currentUser, currentTime);

                    // Same rationale as the per-línea fotos above: the Cotización's fotos generales
                    // remain editable/deletable after conversion, so they're copied here as an
                    // independent snapshot into the Obra's own fotos generales.
                    if (cotizacion.FotosGenerales.Count > 0)
                        await CopiarFotosGeneralesAsync(cotizacion.FotosGenerales, obra.Id, currentUser, currentTime);

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
        IEnumerable<(CotizacionLinea Linea, Actividad Actividad)> pairs, string currentUser, DateTime currentTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var (linea, actividad) in pairs)
        {
            var keyPrefix = $"evidencias/actividad-{actividad.Id}";

            foreach (var foto in linea.Fotos)
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

    private async Task CopiarFotosGeneralesAsync(
        IEnumerable<CotizacionFotoGeneral> fotosGenerales, int obraId, string currentUser, DateTime currentTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var keyPrefix = $"evidencias/obra-{obraId}/generales";

        foreach (var foto in fotosGenerales)
        {
            try
            {
                var copyResult = await _fileStorageService.CopyAsync(foto.FileKey, keyPrefix, ExtensionFromKey(foto.FileKey));
                if (!copyResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Failed to copy cotizacion foto general {FileKey} to obra {ObraId}: {Error}",
                        foto.FileKey, obraId, copyResult.Error);
                    continue;
                }

                string? thumbnailKey = null;
                if (foto.ThumbnailFileKey is not null)
                {
                    var thumbnailCopyResult = await _fileStorageService.CopyAsync(
                        foto.ThumbnailFileKey, $"{keyPrefix}/thumb", ExtensionFromKey(foto.ThumbnailFileKey));
                    thumbnailKey = thumbnailCopyResult.IsSuccess ? thumbnailCopyResult.Value : null;
                }

                context.ObraFotosGenerales.Add(new ObraFotoGeneral
                {
                    ObraId = obraId,
                    RutaArchivo = copyResult.Value!,
                    RutaArchivoThumbnail = thumbnailKey,
                    Descripcion = foto.Descripcion,
                    FechaCarga = foto.FechaCarga,
                    CreatedBy = currentUser,
                    CreatedAt = currentTime,
                    ModifiedBy = currentUser,
                    ModifiedAt = currentTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying cotizacion foto general {FileKey} to obra {ObraId}", foto.FileKey, obraId);
            }
        }

        await context.SaveChangesAsync();
    }

    private static string ExtensionFromKey(string key)
    {
        var extension = Path.GetExtension(key).TrimStart('.');
        return string.IsNullOrEmpty(extension) ? "jpg" : extension;
    }

    public async Task<Result<ObraFotoGeneralDto>> UploadFotoGeneralAsync(
        int obraId, byte[] data, string contentType, string fileName, string? descripcion = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var obraExists = await context.Obras.AnyAsync(o => o.Id == obraId, cancellationToken);
            if (!obraExists)
                return Result<ObraFotoGeneralDto>.Failure(_localizer["Obra not found"]);

            byte[] processedData;
            byte[] thumbnailData;
            string processedContentType;
            using (var stream = new MemoryStream(data))
            {
                (processedData, thumbnailData, processedContentType) = await _imageService.ProcessImageWithThumbnailAsync(
                    stream, fileName, contentType, cancellationToken: cancellationToken);
            }

            var extension = processedContentType switch
            {
                "image/png" => "png",
                "image/webp" => "webp",
                _ => "jpg"
            };

            var keyPrefix = $"evidencias/obra-{obraId}/generales";

            var uploadResult = await _fileStorageService.UploadAsync(
                processedData, processedContentType, keyPrefix, extension, cancellationToken);

            if (!uploadResult.IsSuccess)
                return Result<ObraFotoGeneralDto>.Failure(uploadResult.Error!);

            // The thumbnail is an optimization, not a hard requirement — if it fails to upload,
            // log it and continue without one; the UI falls back to the full image for display.
            var thumbnailUploadResult = await _fileStorageService.UploadAsync(
                thumbnailData, processedContentType, $"{keyPrefix}/thumb", extension, cancellationToken);

            if (!thumbnailUploadResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to upload thumbnail for obra {Id} general foto, continuing with full image only: {Error}",
                    obraId, thumbnailUploadResult.Error);
            }

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var entity = new ObraFotoGeneral
                    {
                        ObraId = obraId,
                        RutaArchivo = uploadResult.Value!,
                        RutaArchivoThumbnail = thumbnailUploadResult.IsSuccess ? thumbnailUploadResult.Value : null,
                        Descripcion = descripcion,
                        FechaCarga = _dateTimeService.Now
                    };

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.ObraFotosGenerales.Add(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var presignedResult = await _fileStorageService.GetPresignedUrlAsync(entity.RutaArchivo, cancellationToken);
                    var thumbnailPresignedResult = entity.RutaArchivoThumbnail is not null
                        ? await _fileStorageService.GetPresignedUrlAsync(entity.RutaArchivoThumbnail, cancellationToken)
                        : null;

                    var resultDto = _mapper.Map<ObraFotoGeneralDto>(entity);
                    resultDto.PresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;
                    resultDto.ThumbnailPresignedUrl = thumbnailPresignedResult?.IsSuccess == true
                        ? thumbnailPresignedResult.Value
                        : resultDto.PresignedUrl;

                    return Result<ObraFotoGeneralDto>.Success(resultDto);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    // Best effort cleanup of the orphaned blob(s) since the DB write failed.
                    await _fileStorageService.DeleteAsync(uploadResult.Value!, cancellationToken);
                    if (thumbnailUploadResult.IsSuccess)
                        await _fileStorageService.DeleteAsync(thumbnailUploadResult.Value!, cancellationToken);
                    throw;
                }
            });
        }
        catch (ArgumentException ex)
        {
            // Thrown by IImageService when the file fails validation (size/type) against
            // the ImageOptions configured in appsettings — surface the specific reason to the user.
            return Result<ObraFotoGeneralDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading general foto for obra {Id}", obraId);
            return Result<ObraFotoGeneralDto>.Failure(_localizer["Error uploading foto"]);
        }
    }

    public async Task<Result> DeleteFotoGeneralAsync(int fotoId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.ObraFotosGenerales.FindAsync([fotoId], cancellationToken);
            if (entity == null)
                return Result.Failure(_localizer["Foto not found"]);

            var key = entity.RutaArchivo;
            var thumbnailKey = entity.RutaArchivoThumbnail;

            var strategy = context.Database.CreateExecutionStrategy();
            var deleteResult = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();
                    context.ObraFotosGenerales.Remove(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (!deleteResult.IsSuccess)
                return deleteResult;

            var storageDeleteResult = await _fileStorageService.DeleteAsync(key, cancellationToken);
            if (!storageDeleteResult.IsSuccess)
                _logger.LogWarning("Failed to delete general foto blob {Key} from storage: {Error}", key, storageDeleteResult.Error);

            if (thumbnailKey is not null)
            {
                var thumbnailDeleteResult = await _fileStorageService.DeleteAsync(thumbnailKey, cancellationToken);
                if (!thumbnailDeleteResult.IsSuccess)
                    _logger.LogWarning("Failed to delete general foto thumbnail {Key} from storage: {Error}", thumbnailKey, thumbnailDeleteResult.Error);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting general foto {Id}", fotoId);
            return Result.Failure(_localizer["Error deleting foto"]);
        }
    }

    public async Task<Result> UpdateFotoGeneralDescripcionAsync(int fotoId, string? descripcion, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.ObraFotosGenerales.FindAsync([fotoId], cancellationToken);
            if (entity == null)
                return Result.Failure(_localizer["Foto not found"]);

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    entity.Descripcion = descripcion;
                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating general foto {Id} descripcion", fotoId);
            return Result.Failure(_localizer["Error updating foto"]);
        }
    }

    public async Task<Result<List<ObraFotoGeneralDto>>> GetFotosGeneralesAsync(int obraId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fotos = await context.ObraFotosGenerales
                .AsNoTracking()
                .Where(f => f.ObraId == obraId)
                .OrderBy(f => f.FechaCarga)
                .ToListAsync(cancellationToken);

            var dtos = new List<ObraFotoGeneralDto>();
            foreach (var foto in fotos)
            {
                var dto = _mapper.Map<ObraFotoGeneralDto>(foto);
                var presignedResult = await _fileStorageService.GetPresignedUrlAsync(foto.RutaArchivo, cancellationToken);
                dto.PresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;

                var thumbnailResult = foto.RutaArchivoThumbnail is not null
                    ? await _fileStorageService.GetPresignedUrlAsync(foto.RutaArchivoThumbnail, cancellationToken)
                    : null;
                dto.ThumbnailPresignedUrl = thumbnailResult?.IsSuccess == true ? thumbnailResult.Value : dto.PresignedUrl;

                dtos.Add(dto);
            }

            return Result<List<ObraFotoGeneralDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving general fotos for obra {Id}", obraId);
            return Result<List<ObraFotoGeneralDto>>.Failure(_localizer["Error retrieving fotos"]);
        }
    }

    public async Task<Result<ObraMensajeDto>> SendMensajeAsync(
        int obraId, TipoObraMensaje tipo, string asunto, string cuerpo,
        byte[]? fotoData = null, string? fotoContentType = null, string? fotoFileName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var obra = await context.Obras
                .Include(o => o.Cliente)
                .FirstOrDefaultAsync(o => o.Id == obraId, cancellationToken);
            if (obra == null)
                return Result<ObraMensajeDto>.Failure(_localizer["Obra not found"]);

            var recipients = ClienteNotificationRecipients.Build(obra.Cliente);
            if (recipients.Count == 0)
                return Result<ObraMensajeDto>.Failure(_localizer["Client has no contact information on file"]);

            string? fotoKey = null;
            string? fotoThumbnailKey = null;
            byte[]? fotoProcessedData = null;
            string? fotoProcessedContentType = null;

            if (fotoData is { Length: > 0 })
            {
                byte[] thumbnailData;
                using (var stream = new MemoryStream(fotoData))
                {
                    (fotoProcessedData, thumbnailData, fotoProcessedContentType) = await _imageService.ProcessImageWithThumbnailAsync(
                        stream, fotoFileName ?? string.Empty, fotoContentType ?? string.Empty, cancellationToken: cancellationToken);
                }

                var extension = fotoProcessedContentType switch
                {
                    "image/png" => "png",
                    "image/webp" => "webp",
                    _ => "jpg"
                };

                var keyPrefix = $"evidencias/obra-{obraId}/mensajes";

                var uploadResult = await _fileStorageService.UploadAsync(
                    fotoProcessedData, fotoProcessedContentType, keyPrefix, extension, cancellationToken);
                if (!uploadResult.IsSuccess)
                    return Result<ObraMensajeDto>.Failure(uploadResult.Error!);

                fotoKey = uploadResult.Value!;

                var thumbnailUploadResult = await _fileStorageService.UploadAsync(
                    thumbnailData, fotoProcessedContentType, $"{keyPrefix}/thumb", extension, cancellationToken);
                if (thumbnailUploadResult.IsSuccess)
                    fotoThumbnailKey = thumbnailUploadResult.Value;
                else
                    _logger.LogWarning(
                        "Failed to upload thumbnail for obra {Id} mensaje foto, continuing with full image only: {Error}",
                        obraId, thumbnailUploadResult.Error);
            }

            var entity = new ObraMensaje
            {
                ObraId = obraId,
                Tipo = tipo,
                Asunto = asunto,
                Cuerpo = cuerpo,
                FotoRutaArchivo = fotoKey,
                FotoRutaArchivoThumbnail = fotoThumbnailKey,
                Destinatarios = ClienteNotificationRecipients.Describe(recipients),
                FechaEnvio = _dateTimeService.Now
            };

            var strategy = context.Database.CreateExecutionStrategy();
            var saveResult = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.ObraMensajes.Add(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Result<ObraMensaje>.Success(entity);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (!saveResult.IsSuccess)
            {
                if (fotoKey is not null)
                    await _fileStorageService.DeleteAsync(fotoKey, cancellationToken);
                if (fotoThumbnailKey is not null)
                    await _fileStorageService.DeleteAsync(fotoThumbnailKey, cancellationToken);
                return Result<ObraMensajeDto>.Failure(saveResult.Error!);
            }

            var attachments = new List<NotificationAttachment>();
            if (fotoProcessedData is not null)
            {
                attachments.Add(new NotificationAttachment
                {
                    FileName = fotoFileName ?? $"foto.{ExtensionFromKey(fotoKey!)}",
                    Content = fotoProcessedData,
                    ContentType = fotoProcessedContentType!
                });
            }

            var message = new NotificationMessage
            {
                EventType = tipo == TipoObraMensaje.Alerta ? "ObraAlerta" : "ObraMensaje",
                RelatedEntityType = nameof(Obra),
                RelatedEntityId = obraId,
                Subject = tipo == TipoObraMensaje.Alerta ? _localizer["Alert: {0}", asunto] : asunto,
                Body = cuerpo,
                Recipients = recipients,
                Attachments = attachments
            };

            try
            {
                await _notificationService.NotifyAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                // Delivery is best-effort — the message is already saved to the Obra's history, so a
                // notification failure here must not surface as a failure of the overall operation.
                _logger.LogError(ex, "Error notifying obra {Id} mensaje", obraId);
            }

            var dto = _mapper.Map<ObraMensajeDto>(entity);
            if (fotoKey is not null)
            {
                var presignedResult = await _fileStorageService.GetPresignedUrlAsync(fotoKey, cancellationToken);
                dto.FotoPresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;

                var thumbnailPresignedResult = fotoThumbnailKey is not null
                    ? await _fileStorageService.GetPresignedUrlAsync(fotoThumbnailKey, cancellationToken)
                    : null;
                dto.FotoThumbnailPresignedUrl = thumbnailPresignedResult?.IsSuccess == true
                    ? thumbnailPresignedResult.Value
                    : dto.FotoPresignedUrl;
            }

            return Result<ObraMensajeDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            // Thrown by IImageService when the file fails validation (size/type) against
            // the ImageOptions configured in appsettings — surface the specific reason to the user.
            return Result<ObraMensajeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mensaje for obra {Id}", obraId);
            return Result<ObraMensajeDto>.Failure(_localizer["Error sending mensaje"]);
        }
    }

    public async Task<Result<List<ObraMensajeDto>>> GetMensajesAsync(int obraId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var mensajes = await context.ObraMensajes
                .AsNoTracking()
                .Where(m => m.ObraId == obraId)
                .OrderByDescending(m => m.FechaEnvio)
                .ToListAsync(cancellationToken);

            var dtos = new List<ObraMensajeDto>();
            foreach (var mensaje in mensajes)
            {
                var dto = _mapper.Map<ObraMensajeDto>(mensaje);

                if (mensaje.FotoRutaArchivo is not null)
                {
                    var presignedResult = await _fileStorageService.GetPresignedUrlAsync(mensaje.FotoRutaArchivo, cancellationToken);
                    dto.FotoPresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;

                    var thumbnailResult = mensaje.FotoRutaArchivoThumbnail is not null
                        ? await _fileStorageService.GetPresignedUrlAsync(mensaje.FotoRutaArchivoThumbnail, cancellationToken)
                        : null;
                    dto.FotoThumbnailPresignedUrl = thumbnailResult?.IsSuccess == true ? thumbnailResult.Value : dto.FotoPresignedUrl;
                }

                dtos.Add(dto);
            }

            return Result<List<ObraMensajeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mensajes for obra {Id}", obraId);
            return Result<List<ObraMensajeDto>>.Failure(_localizer["Error retrieving mensajes"]);
        }
    }
}
