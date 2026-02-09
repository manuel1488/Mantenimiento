using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IInventoryQueryService
{
    /// <summary>
    /// Gets paginated inventory status with various filters
    /// </summary>
    Task<(int TotalCount, IList<InventoryDto> Items)> GetInventoryStatusAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        int? warehouseId = null,
        bool? hasStock = null,
        bool? belowMinStock = null,
        bool? aboveMaxStock = null,
        bool? activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product stock across all warehouses or a specific one
    /// </summary>
    Task<ProductStockDto?> GetProductStockAsync(
        long productId,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets warehouse stock summary
    /// </summary>
    Task<WarehouseStockDto> GetWarehouseStockAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);
}