using AutoMapper;

using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class InventoryQueryService : IInventoryQueryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryQueryService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public InventoryQueryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<InventoryQueryService> logger,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<(int TotalCount, IList<InventoryDto> Items)> GetInventoryStatusAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        int? locationId = null,
        bool? hasStock = null,
        bool? belowMinStock = null,
        bool? aboveMaxStock = null,
        bool? activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (locationId.HasValue && !await _currentUserService.HasAccessToLocationAsync(locationId.Value))
            {
                return (0, new List<InventoryDto>());
            }

            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<App.Models.Shop.Inventory> query = _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(p => p.UnitMeasure)
                .Include(x => x.Location)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Product.Name.Contains(searchString) ||
                    x.Product.Code.Contains(searchString) ||
                    x.Product.Brand.Contains(searchString));
            }

            if (locationId.HasValue)
            {
                query = query.Where(x => x.LocationId == locationId.Value);
            }
            else if (!await _currentUserService.GetIsGlobalAccessAsync())
            {
                var assignedIds = await _currentUserService.GetAssignedLocationIdsAsync();
                query = query.Where(x => assignedIds.Contains(x.LocationId));
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
                    x.Location.IsActive);
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
        int? locationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Products that don't require inventory are always available
            var product = await _context.Products
                .AsNoTracking()
                .Where(x => x.Id == productId)
                .Select(x => new { x.Id, x.Name, x.Code, x.RequiresInventory, UnitMeasureName = x.UnitMeasure.Name })
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
                return null;

            if (!product.RequiresInventory)
            {
                return new ProductStockDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code,
                    UnitMeasureName = product.UnitMeasureName,
                    RequiresInventory = false,
                    TotalStock = decimal.MaxValue,
                    LocationStock = []
                };
            }

            var query = _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Include(x => x.Location)
                .Where(x => x.ProductId == productId)
                .AsNoTracking();

            if (locationId.HasValue)
            {
                query = query.Where(x => x.LocationId == locationId.Value);
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
                RequiresInventory = true,
                TotalStock = inventoryItems.Sum(x => x.GetAvailableIndividualUnits()),
                LocationStock = inventoryItems.Select(x => new ProductWarehouseStockDto
                {
                    LocationId = x.LocationId,
                    LocationName = x.Location.Name,
                    LocationType = x.Location.Type,
                    Quantity = x.Quantity,
                    IndividualUnits = x.GetAvailableIndividualUnits(),
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

    public async Task<WarehouseStockDto> GetLocationStockAsync(
        int locationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _currentUserService.HasAccessToLocationAsync(locationId))
                throw new InvalidOperationException($"Location not found: {locationId}");

            await using var _context = await _contextFactory.CreateDbContextAsync();

            var location = await _context.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken);

            if (location == null)
                throw new InvalidOperationException($"Location not found: {locationId}");

            var inventory = await _context.Inventory
                .Include(x => x.Product)
                .ThenInclude(x => x.UnitMeasure)
                .Where(x => x.LocationId == locationId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var dto = new WarehouseStockDto
            {
                LocationId = location.Id,
                LocationName = location.Name,
                LocationType = location.Type,
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
            _logger.LogError(ex, "Error getting location stock for location {LocationId}", locationId);
            throw;
        }
    }

    #region Helper Methods for Unit Conversion

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