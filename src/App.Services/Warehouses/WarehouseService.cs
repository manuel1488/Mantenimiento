using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Warehouse;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Warehouses;

public class WarehouseService : IWarehouseService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<WarehouseService> _logger;
    private readonly IStringLocalizer<WarehouseService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public WarehouseService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<WarehouseService> logger,
        IStringLocalizer<WarehouseService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<(int TotalCount, IList<WarehouseDto> Items)> GetWarehousesAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Warehouse> query = _context.Warehouses
                .Include(w => w.Branch)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    (x.Description != null && x.Description.Contains(searchString)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<WarehouseDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses");
            throw;
        }
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var warehouse = await _context.Warehouses
                .Include(w => w.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return warehouse != null ? _mapper.Map<WarehouseDto>(warehouse) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse by id {Id}", id);
            throw;
        }
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Check if warehouse with same name already exists
            var exists = await _context.Warehouses
                .AnyAsync(x => x.Name == createDto.Name);

            if (exists)
            {
                throw new InvalidOperationException(
                    L["Warehouse with name {0} already exists", createDto.Name]);
            }

            var warehouse = _mapper.Map<Warehouse>(createDto);

            // Set audit fields
            warehouse.CreatedBy = _currentUserService.FullName ?? "Unknown";
            warehouse.CreatedAt = _dateTime.Now;

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return _mapper.Map<WarehouseDto>(warehouse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse");
            throw;
        }
    }

    public async Task<WarehouseDto> UpdateWarehouseAsync(int id, UpdateWarehouseDto updateDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(x => x.Id == id);

            if (warehouse == null)
            {
                throw new InvalidOperationException(
                    L["Warehouse not found with ID {0}", id]);
            }

            // Check if name is being changed and if new one already exists
            if (updateDto.Name != warehouse.Name)
            {
                var exists = await _context.Warehouses
                    .AnyAsync(x => x.Id != id && x.Name == updateDto.Name);

                if (exists)
                {
                    throw new InvalidOperationException(
                        L["Warehouse with name {0} already exists", updateDto.Name]);
                }
            }

            // Update properties
            _mapper.Map(updateDto, warehouse);

            // Update audit fields
            warehouse.ModifiedBy = _currentUserService.FullName ?? "Unknown";
            warehouse.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            return _mapper.Map<WarehouseDto>(warehouse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteWarehouseAsync(int id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(x => x.Id == id);

            if (warehouse == null)
            {
                return false;
            }

            // Check if warehouse has related records
            var hasInventory = await _context.Inventory
                .AnyAsync(x => x.WarehouseId == id);

            if (hasInventory)
            {
                throw new InvalidOperationException(
                    L["Cannot delete warehouse because it has inventory records"]);
            }

            warehouse.DeletedBy = _currentUserService.FullName ?? "Unknown";
            warehouse.DeletedAt = _dateTime.Now;
            warehouse.IsDeleted = 1;

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warehouse {Id}", id);
            throw;
        }
    }

    public async Task<bool> ValidateUniqueNameAsync(string name, int? excludeId = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.Warehouses.AsNoTracking();

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(x => x.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating warehouse name uniqueness");
            throw;
        }
    }
}