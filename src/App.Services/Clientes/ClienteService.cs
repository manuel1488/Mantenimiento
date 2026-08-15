using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.Common;
using App.Core.DTOs.Clientes;
using App.Core.Interfaces;
using App.Models.Clientes;
using App.Models.Data.Contexts;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Clientes;

public class ClienteService : IClienteService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ClienteService> _logger;
    private readonly IStringLocalizer<ClienteService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public ClienteService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ClienteService> logger,
        IStringLocalizer<ClienteService> localizer,
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

    public async Task<Result<List<ClienteDto>>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var clientes = await context.Clientes
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ProjectTo<ClienteDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<ClienteDto>>.Success(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clientes");
            return Result<List<ClienteDto>>.Failure(_localizer["Error retrieving clientes"]);
        }
    }

    public async Task<Result<ClienteDto>> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cliente = await context.Clientes.FindAsync(id);
            if (cliente == null)
                return Result<ClienteDto>.Failure(_localizer["Cliente not found"]);

            return Result<ClienteDto>.Success(_mapper.Map<ClienteDto>(cliente));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cliente {Id}", id);
            return Result<ClienteDto>.Failure(_localizer["Error retrieving cliente"]);
        }
    }

    public async Task<Result<ClienteDto>> CreateAsync(CreateClienteDto dto)
    {
        if (dto.TieneDatosFiscales && string.IsNullOrWhiteSpace(dto.RazonSocial))
            return Result<ClienteDto>.Failure(_localizer["Legal name is required when the client has fiscal data"]);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    if (!string.IsNullOrWhiteSpace(dto.Rfc) &&
                        await context.Clientes.AnyAsync(c => c.Rfc == dto.Rfc))
                    {
                        await transaction.RollbackAsync();
                        return Result<ClienteDto>.Failure(_localizer["A client with this RFC already exists"]);
                    }

                    var entity = _mapper.Map<Cliente>(dto);

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.Clientes.Add(entity);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<ClienteDto>.Success(_mapper.Map<ClienteDto>(entity));
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
            _logger.LogError(ex, "Error creating cliente");
            return Result<ClienteDto>.Failure(_localizer["Error creating cliente"]);
        }
    }

    public async Task<Result<ClienteDto>> UpdateAsync(int id, UpdateClienteDto dto)
    {
        if (dto.TieneDatosFiscales && string.IsNullOrWhiteSpace(dto.RazonSocial))
            return Result<ClienteDto>.Failure(_localizer["Legal name is required when the client has fiscal data"]);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var entity = await context.Clientes.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<ClienteDto>.Failure(_localizer["Cliente not found"]);
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Rfc) &&
                        await context.Clientes.AnyAsync(c => c.Rfc == dto.Rfc && c.Id != id))
                    {
                        await transaction.RollbackAsync();
                        return Result<ClienteDto>.Failure(_localizer["A client with this RFC already exists"]);
                    }

                    _mapper.Map(dto, entity);

                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<ClienteDto>.Success(_mapper.Map<ClienteDto>(entity));
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
            _logger.LogError(ex, "Error updating cliente {Id}", id);
            return Result<ClienteDto>.Failure(_localizer["Error updating cliente"]);
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
                    var entity = await context.Clientes.FindAsync(id);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Cliente not found"]);
                    }

                    if (await context.Obras.AnyAsync(o => o.ClienteId == id))
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Cannot delete a client with existing Obras"]);
                    }

                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();

                    context.Clientes.Remove(entity);
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
            _logger.LogError(ex, "Error deleting cliente {Id}", id);
            return Result.Failure(_localizer["Error deleting cliente"]);
        }
    }
}
