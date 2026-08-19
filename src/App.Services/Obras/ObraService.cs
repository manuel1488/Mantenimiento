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

public class ObraService : IObraService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ObraService> _logger;
    private readonly IStringLocalizer<ObraService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public ObraService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ObraService> logger,
        IStringLocalizer<ObraService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
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
}
