using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.Common;
using App.Core.DTOs.Obras;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Obras;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Obras;

public class ActividadService : IActividadService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ActividadService> _logger;
    private readonly IStringLocalizer<ActividadService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;
    private readonly IImageService _imageService;
    private readonly IFileStorageService _fileStorageService;

    public ActividadService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ActividadService> logger,
        IStringLocalizer<ActividadService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        IImageService imageService,
        IFileStorageService fileStorageService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _imageService = imageService;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<List<ActividadDto>>> GetByObraIdAsync(int obraId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var actividades = await context.Actividades
                .AsNoTracking()
                .Include(a => a.Servicio).ThenInclude(s => s.UnidadMedida)
                .Where(a => a.ObraId == obraId)
                .ProjectTo<ActividadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<ActividadDto>>.Success(actividades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving actividades for obra {ObraId}", obraId);
            return Result<List<ActividadDto>>.Failure(_localizer["Error retrieving actividades"]);
        }
    }

    public async Task<Result<ActividadDto>> CreateAsync(CreateActividadDto dto)
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
                    var obra = await context.Obras.FindAsync(dto.ObraId);
                    if (obra == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Obra not found"]);
                    }

                    if (obra.Estado is ObraEstado.Finalizada or ObraEstado.Facturada)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Cannot add Actividades to an Obra that is Finalizada or Facturada"]);
                    }

                    var servicio = await context.Servicios.FindAsync(dto.ServicioId);
                    if (servicio == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Servicio not found"]);
                    }

                    var precioUnitario = dto.PrecioUnitarioOverride ?? servicio.PrecioUnitario;
                    var rendimiento = dto.RendimientoDiasPorUnidadOverride ?? servicio.RendimientoDiasPorUnidad;

                    var entity = new Actividad
                    {
                        ObraId = dto.ObraId,
                        ServicioId = dto.ServicioId,
                        Descripcion = dto.Descripcion ?? servicio.Descripcion,
                        Cantidad = dto.Cantidad,
                        PrecioUnitario = precioUnitario,
                        Costo = dto.Cantidad * precioUnitario,
                        RendimientoDiasPorUnidad = rendimiento,
                        TiempoEstimadoDias = dto.Cantidad * rendimiento,
                        Estado = ActividadEstado.Pendiente
                    };

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.Actividades.Add(entity);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var created = await context.Actividades
                        .AsNoTracking()
                        .Include(a => a.Servicio).ThenInclude(s => s.UnidadMedida)
                        .FirstAsync(a => a.Id == entity.Id);

                    return Result<ActividadDto>.Success(_mapper.Map<ActividadDto>(created));
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
            _logger.LogError(ex, "Error creating actividad");
            return Result<ActividadDto>.Failure(_localizer["Error creating actividad"]);
        }
    }

    public async Task<Result<ActividadDto>> UpdateAsync(UpdateActividadDto dto)
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
                    var entity = await context.Actividades
                        .Include(a => a.Obra)
                        .FirstOrDefaultAsync(a => a.Id == dto.Id);

                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Actividad not found"]);
                    }

                    if (entity.Obra.Estado is ObraEstado.Finalizada or ObraEstado.Facturada)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Cannot modify Actividades of an Obra that is Finalizada or Facturada"]);
                    }

                    entity.Cantidad = dto.Cantidad;
                    entity.Descripcion = dto.Descripcion;
                    if (dto.PrecioUnitarioOverride.HasValue)
                        entity.PrecioUnitario = dto.PrecioUnitarioOverride.Value;
                    if (dto.RendimientoDiasPorUnidadOverride.HasValue)
                        entity.RendimientoDiasPorUnidad = dto.RendimientoDiasPorUnidadOverride.Value;

                    entity.Costo = entity.Cantidad * entity.PrecioUnitario;
                    entity.TiempoEstimadoDias = entity.Cantidad * entity.RendimientoDiasPorUnidad;

                    var currentTime = _dateTimeService.Now;
                    ApplyPorcentajeAvance(entity, dto.PorcentajeAvance, currentTime);

                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = currentTime;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Actividades
                        .AsNoTracking()
                        .Include(a => a.Servicio).ThenInclude(s => s.UnidadMedida)
                        .FirstAsync(a => a.Id == entity.Id);

                    return Result<ActividadDto>.Success(_mapper.Map<ActividadDto>(updated));
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
            _logger.LogError(ex, "Error updating actividad {Id}", dto.Id);
            return Result<ActividadDto>.Failure(_localizer["Error updating actividad"]);
        }
    }

    public async Task<Result<ActividadDto>> ActualizarAvanceAsync(int id, int porcentajeAvance)
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
                    var entity = await context.Actividades
                        .Include(a => a.Obra)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Actividad not found"]);
                    }

                    if (entity.Obra.Estado is ObraEstado.Finalizada or ObraEstado.Facturada)
                    {
                        await transaction.RollbackAsync();
                        return Result<ActividadDto>.Failure(_localizer["Cannot modify Actividades of an Obra that is Finalizada or Facturada"]);
                    }

                    var currentTime = _dateTimeService.Now;
                    ApplyPorcentajeAvance(entity, porcentajeAvance, currentTime);

                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = currentTime;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Actividades
                        .AsNoTracking()
                        .Include(a => a.Servicio).ThenInclude(s => s.UnidadMedida)
                        .FirstAsync(a => a.Id == entity.Id);

                    return Result<ActividadDto>.Success(_mapper.Map<ActividadDto>(updated));
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
            _logger.LogError(ex, "Error updating progress for actividad {Id}", id);
            return Result<ActividadDto>.Failure(_localizer["Error updating actividad"]);
        }
    }

    private static void ApplyPorcentajeAvance(Actividad entity, int porcentajeAvance, DateTime currentTime)
    {
        var clamped = Math.Clamp(porcentajeAvance, 0, 100);
        entity.PorcentajeAvance = clamped;
        entity.Estado = clamped switch
        {
            0 => ActividadEstado.Pendiente,
            100 => ActividadEstado.Finalizada,
            _ => ActividadEstado.EnProceso
        };
        if (entity.Estado != ActividadEstado.Pendiente && entity.FechaInicio is null)
            entity.FechaInicio = currentTime;
        entity.FechaFin = entity.Estado == ActividadEstado.Finalizada ? currentTime : null;
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
                    var entity = await context.Actividades.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Actividad not found"]);
                    }

                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();

                    context.Actividades.Remove(entity);
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
            _logger.LogError(ex, "Error deleting actividad {Id}", id);
            return Result.Failure(_localizer["Error deleting actividad"]);
        }
    }

    public async Task<Result<ActividadEvidenciaFotoDto>> UploadEvidenciaAsync(
        int actividadId,
        TipoEvidencia tipo,
        byte[] data,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var actividadExists = await context.Actividades.AnyAsync(a => a.Id == actividadId, cancellationToken);
            if (!actividadExists)
                return Result<ActividadEvidenciaFotoDto>.Failure(_localizer["Actividad not found"]);

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

            var keyPrefix = $"evidencias/actividad-{actividadId}";

            var uploadResult = await _fileStorageService.UploadAsync(
                processedData, processedContentType, keyPrefix, extension, cancellationToken);

            if (!uploadResult.IsSuccess)
                return Result<ActividadEvidenciaFotoDto>.Failure(uploadResult.Error!);

            // The thumbnail is an optimization, not a hard requirement — if it fails to upload,
            // log it and continue without one; the UI falls back to the full image for display.
            var thumbnailUploadResult = await _fileStorageService.UploadAsync(
                thumbnailData, processedContentType, $"{keyPrefix}/thumb", extension, cancellationToken);

            if (!thumbnailUploadResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to upload thumbnail for actividad {Id}, continuing with full image only: {Error}",
                    actividadId, thumbnailUploadResult.Error);
            }

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var entity = new ActividadEvidenciaFoto
                    {
                        ActividadId = actividadId,
                        Tipo = tipo,
                        RutaArchivo = uploadResult.Value!,
                        RutaArchivoThumbnail = thumbnailUploadResult.IsSuccess ? thumbnailUploadResult.Value : null,
                        FechaCarga = _dateTimeService.Now
                    };

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.ActividadEvidenciaFotos.Add(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var presignedResult = await _fileStorageService.GetPresignedUrlAsync(entity.RutaArchivo, cancellationToken);
                    var thumbnailPresignedResult = entity.RutaArchivoThumbnail is not null
                        ? await _fileStorageService.GetPresignedUrlAsync(entity.RutaArchivoThumbnail, cancellationToken)
                        : null;

                    var resultDto = _mapper.Map<ActividadEvidenciaFotoDto>(entity);
                    resultDto.PresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;
                    resultDto.ThumbnailPresignedUrl = thumbnailPresignedResult?.IsSuccess == true
                        ? thumbnailPresignedResult.Value
                        : resultDto.PresignedUrl;

                    return Result<ActividadEvidenciaFotoDto>.Success(resultDto);
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
            return Result<ActividadEvidenciaFotoDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading evidencia for actividad {Id}", actividadId);
            return Result<ActividadEvidenciaFotoDto>.Failure(_localizer["Error uploading evidencia"]);
        }
    }

    public async Task<Result> DeleteEvidenciaAsync(int evidenciaId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.ActividadEvidenciaFotos.FindAsync(evidenciaId);
            if (entity == null)
                return Result.Failure(_localizer["Evidencia not found"]);

            var key = entity.RutaArchivo;
            var thumbnailKey = entity.RutaArchivoThumbnail;

            var strategy = context.Database.CreateExecutionStrategy();
            var deleteResult = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();
                    context.ActividadEvidenciaFotos.Remove(entity);
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

            if (!deleteResult.IsSuccess)
                return deleteResult;

            var storageDeleteResult = await _fileStorageService.DeleteAsync(key);
            if (!storageDeleteResult.IsSuccess)
                _logger.LogWarning("Failed to delete evidencia blob {Key} from storage: {Error}", key, storageDeleteResult.Error);

            if (thumbnailKey is not null)
            {
                var thumbnailDeleteResult = await _fileStorageService.DeleteAsync(thumbnailKey);
                if (!thumbnailDeleteResult.IsSuccess)
                    _logger.LogWarning("Failed to delete evidencia thumbnail {Key} from storage: {Error}", thumbnailKey, thumbnailDeleteResult.Error);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting evidencia {Id}", evidenciaId);
            return Result.Failure(_localizer["Error deleting evidencia"]);
        }
    }

    public async Task<Result<List<ActividadEvidenciaFotoDto>>> GetEvidenciasAsync(int actividadId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var evidencias = await context.ActividadEvidenciaFotos
                .AsNoTracking()
                .Where(e => e.ActividadId == actividadId)
                .OrderBy(e => e.FechaCarga)
                .ToListAsync();

            var dtos = new List<ActividadEvidenciaFotoDto>();
            foreach (var evidencia in evidencias)
            {
                var dto = _mapper.Map<ActividadEvidenciaFotoDto>(evidencia);
                var presignedResult = await _fileStorageService.GetPresignedUrlAsync(evidencia.RutaArchivo);
                dto.PresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;

                var thumbnailResult = evidencia.RutaArchivoThumbnail is not null
                    ? await _fileStorageService.GetPresignedUrlAsync(evidencia.RutaArchivoThumbnail)
                    : null;
                dto.ThumbnailPresignedUrl = thumbnailResult?.IsSuccess == true ? thumbnailResult.Value : dto.PresignedUrl;

                dtos.Add(dto);
            }

            return Result<List<ActividadEvidenciaFotoDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidencias for actividad {Id}", actividadId);
            return Result<List<ActividadEvidenciaFotoDto>>.Failure(_localizer["Error retrieving evidencias"]);
        }
    }
}
