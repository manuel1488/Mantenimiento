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
        int? locationId = null,
        bool? hasStock = null,
        bool? belowMinStock = null,
        bool? aboveMaxStock = null,
        bool? activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product stock across all locations or a specific one
    /// </summary>
    Task<ProductStockDto?> GetProductStockAsync(
        long productId,
        int? locationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets location stock summary
    /// </summary>
    Task<WarehouseStockDto> GetLocationStockAsync(
        int locationId,
        CancellationToken cancellationToken = default);
}