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

public class UnidadMedidaService : IUnidadMedidaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<UnidadMedidaService> _logger;
    private readonly IStringLocalizer<UnidadMedidaService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public UnidadMedidaService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<UnidadMedidaService> logger,
        IStringLocalizer<UnidadMedidaService> localizer,
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

    public async Task<Result<List<UnidadMedidaDto>>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var unidades = await context.UnidadesMedida
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .ProjectTo<UnidadMedidaDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<UnidadMedidaDto>>.Success(unidades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unidades de medida");
            return Result<List<UnidadMedidaDto>>.Failure(_localizer["Error retrieving unit measures"]);
        }
    }

    public async Task<Result<UnidadMedidaDto>> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var unidad = await context.UnidadesMedida
                .AsNoTracking()
                .Where(u => u.Id == id)
                .ProjectTo<UnidadMedidaDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (unidad == null)
                return Result<UnidadMedidaDto>.Failure(_localizer["Unit measure not found"]);

            return Result<UnidadMedidaDto>.Success(unidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unidad de medida {Id}", id);
            return Result<UnidadMedidaDto>.Failure(_localizer["Error retrieving unit measure"]);
        }
    }

    public async Task<Result<UnidadMedidaDto>> CreateAsync(CreateUnidadMedidaDto dto)
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
                    var codeExists = await context.UnidadesMedida
                        .AnyAsync(u => u.Codigo == dto.Codigo);
                    if (codeExists)
                    {
                        await transaction.RollbackAsync();
                        return Result<UnidadMedidaDto>.Failure(_localizer["A unit measure with this code already exists"]);
                    }

                    var entity = _mapper.Map<UnidadMedida>(dto);

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.UnidadesMedida.Add(entity);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var created = await GetByIdAsync(entity.Id);
                    return created;
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
            _logger.LogError(ex, "Error creating unidad de medida");
            return Result<UnidadMedidaDto>.Failure(_localizer["Error creating unit measure"]);
        }
    }

    public async Task<Result<UnidadMedidaDto>> UpdateAsync(int id, UpdateUnidadMedidaDto dto)
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
                    var entity = await context.UnidadesMedida.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<UnidadMedidaDto>.Failure(_localizer["Unit measure not found"]);
                    }

                    var codeExists = await context.UnidadesMedida
                        .AnyAsync(u => u.Id != id && u.Codigo == dto.Codigo);
                    if (codeExists)
                    {
                        await transaction.RollbackAsync();
                        return Result<UnidadMedidaDto>.Failure(_localizer["A unit measure with this code already exists"]);
                    }

                    _mapper.Map(dto, entity);

                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return await GetByIdAsync(id);
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
            _logger.LogError(ex, "Error updating unidad de medida {Id}", id);
            return Result<UnidadMedidaDto>.Failure(_localizer["Error updating unit measure"]);
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
                    var entity = await context.UnidadesMedida.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Unit measure not found"]);
                    }

                    var inUse = await context.Servicios.AnyAsync(s => s.UnidadMedidaId == id);
                    if (inUse)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Cannot delete a unit measure that is being used by a service"]);
                    }

                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();

                    context.UnidadesMedida.Remove(entity);
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
            _logger.LogError(ex, "Error deleting unidad de medida {Id}", id);
            return Result.Failure(_localizer["Error deleting unit measure"]);
        }
    }
}
