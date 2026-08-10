using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.Common;
using App.Core.DTOs.Servicios;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Servicios;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Servicios;

public class ServicioService : IServicioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ServicioService> _logger;
    private readonly IStringLocalizer<ServicioService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public ServicioService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ServicioService> logger,
        IStringLocalizer<ServicioService> localizer,
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

    public async Task<Result<List<ServicioDto>>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var servicios = await context.Servicios
                .AsNoTracking()
                .OrderBy(s => s.Nombre)
                .ProjectTo<ServicioDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<ServicioDto>>.Success(servicios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving servicios");
            return Result<List<ServicioDto>>.Failure(_localizer["Error retrieving servicios"]);
        }
    }

    public async Task<Result<ServicioDto>> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var servicio = await context.Servicios.FindAsync(id);
            if (servicio == null)
                return Result<ServicioDto>.Failure(_localizer["Servicio not found"]);

            return Result<ServicioDto>.Success(_mapper.Map<ServicioDto>(servicio));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving servicio {Id}", id);
            return Result<ServicioDto>.Failure(_localizer["Error retrieving servicio"]);
        }
    }

    public async Task<Result<ServicioDto>> CreateAsync(CreateServicioDto dto)
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
                    var entity = _mapper.Map<Servicio>(dto);

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.Servicios.Add(entity);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<ServicioDto>.Success(_mapper.Map<ServicioDto>(entity));
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
            _logger.LogError(ex, "Error creating servicio");
            return Result<ServicioDto>.Failure(_localizer["Error creating servicio"]);
        }
    }

    public async Task<Result<ServicioDto>> UpdateAsync(int id, UpdateServicioDto dto)
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
                    var entity = await context.Servicios.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ServicioDto>.Failure(_localizer["Servicio not found"]);
                    }

                    _mapper.Map(dto, entity);

                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<ServicioDto>.Success(_mapper.Map<ServicioDto>(entity));
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
            _logger.LogError(ex, "Error updating servicio {Id}", id);
            return Result<ServicioDto>.Failure(_localizer["Error updating servicio"]);
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
                    var entity = await context.Servicios.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Servicio not found"]);
                    }

                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();

                    context.Servicios.Remove(entity);
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
            _logger.LogError(ex, "Error deleting servicio {Id}", id);
            return Result.Failure(_localizer["Error deleting servicio"]);
        }
    }
}
