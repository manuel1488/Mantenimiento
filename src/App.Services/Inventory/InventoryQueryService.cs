using AutoMapper;

using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class InventoryQueryService : IInventoryQueryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryQueryService> _logger;

    public InventoryQueryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<InventoryQueryService> logger)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(int TotalCount, IList<InventoryDto> Items)> GetInventoryStatusAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        int? warehouseId = null,
        bool? hasStock = null,
        bool? belowMinStock = null,
        bool? aboveMaxStock = null,
        bool? activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<App.Models.Shop.Inventory> query = _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(p => p.UnitMeasure)
                .Include(x => x.Warehouse)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Product.Name.Contains(searchString) ||
                    x.Product.Code.Contains(searchString) ||
                    x.Product.Brand.Contains(searchString));
            }

            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            }

            if (hasStock.HasValue)
            {
                query = hasStock.Value
                    ? query.Where(x => x.Quantity > 0)
                    : query.Where(x => x.Quantity == 0);
            }

            if (belowMinStock.HasValue && belowMinStock.Value)
            {
                query = query.Where(x =>
                    x.MinStock.HasValue &&
                    x.Quantity < x.MinStock.Value);
            }

            if (aboveMaxStock.HasValue && aboveMaxStock.Value)
            {
                query = query.Where(x =>
                    x.MaxStock.HasValue &&
                    x.Quantity > x.MaxStock.Value);
            }

            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(x =>
                    x.Product.IsActive &&
                    x.Warehouse.IsActive);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .OrderBy(x => x.Product.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<InventoryDto>(x))
                .ToListAsync(cancellationToken);

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory status");
            throw;
        }
    }

    public async Task<ProductStockDto?>  GetProductStockAsync(
        long productId,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Include(x => x.Warehouse)
                .Where(x => x.ProductId == productId)
                .AsNoTracking();

            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            }

            var inventoryItems = await query.ToListAsync(cancellationToken);
            
            if (!inventoryItems.Any())
                return null;

            var firstItem = inventoryItems.First();
            
            return new ProductStockDto
            {
                ProductId = firstItem.ProductId,
                ProductName = firstItem.Product.Name,
                ProductCode = firstItem.Product.Code,
                UnitMeasureName = firstItem.Product.UnitMeasure.Name,
                TotalStock = inventoryItems.Sum(x => GetAvailableIndividualUnits(x)),
                WarehouseStock = inventoryItems.Select(x => new ProductWarehouseStockDto
                {
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.Warehouse.Name,
                    Quantity = GetAvailableIndividualUnits(x),
                    MinStock = x.MinStock.HasValue ? GetIndividualUnitsFromContainer(x.Product, x.MinStock.Value) : null,
                    MaxStock = x.MaxStock.HasValue ? GetIndividualUnitsFromContainer(x.Product, x.MaxStock.Value) : null
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product stock for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<WarehouseStockDto> GetWarehouseStockAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var warehouse = await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == warehouseId, cancellationToken);

            if (warehouse == null)
                throw new InvalidOperationException($"Warehouse not found: {warehouseId}");

            var inventory = await _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Where(x => x.WarehouseId == warehouseId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var dto = new WarehouseStockDto
            {
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name,
                TotalProducts = inventory.Count,
                ProductsWithStock = inventory.Count(x => x.Quantity > 0),
                ProductsBelowMinStock = inventory.Count(x =>
                    x.MinStock.HasValue && x.Quantity < x.MinStock.Value),
                ProductsAboveMaxStock = inventory.Count(x =>
                    x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value),
                ProductStock = inventory.Select(x => new WarehouseProductStockDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    ProductCode = x.Product.Code,
                    UnitMeasureName = x.Product.UnitMeasure.Name,
                    Quantity = x.Quantity,
                    MinStock = x.MinStock,
                    MaxStock = x.MaxStock,
                    IsBelowMinStock = x.MinStock.HasValue && x.Quantity < x.MinStock.Value,
                    IsAboveMaxStock = x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value
                }).ToList()
            };

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse stock for warehouse {WarehouseId}", warehouseId);
            throw;
        }
    }

    #region Helper Methods for Unit Conversion

    /// <summary>
    /// Gets the available individual units (e.g., liters) from container units stored in inventory
    /// </summary>
    /// <param name="inventory">Inventory record with Product included</param>
    /// <returns>Available individual units</returns>
    private decimal GetAvailableIndividualUnits(App.Models.Shop.Inventory inventory)
    {
        // Use new field if available
        if (inventory.IndividualUnits > 0)
        {
            return inventory.IndividualUnits;
        }

        // Fallback to legacy calculation
        // If product allows partial sales and has content, calculate individual units
        if (inventory.Product.IsPartialSaleAllowed && inventory.Product.Content > 0)
        {
            // inventory.Quantity = containers, Product.Content = units per container
            return inventory.Quantity * inventory.Product.Content;
        }

        // For products without partial sales, quantity is already in individual units
        return inventory.Quantity;
    }

    /// <summary>
    /// Converts container units to individual units for display
    /// </summary>
    /// <param name="product">Product with IsPartialSaleAllowed and Content</param>
    /// <param name="containerUnits">Container units to convert</param>
    /// <returns>Individual units</returns>
    private decimal GetIndividualUnitsFromContainer(App.Models.Shop.Product product, decimal containerUnits)
    {
        // If product allows partial sales and has content, convert to individual units
        if (product.IsPartialSaleAllowed && product.Content > 0)
        {
            return containerUnits * product.Content;
        }

        // For products without partial sales, container units are the same as individual units
        return containerUnits;
    }

    #endregion
}