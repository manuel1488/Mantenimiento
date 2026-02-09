using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IInventoryHistoryService
{
    /// <summary>
    /// Gets the movement history for a specific product
    /// </summary>
    Task<IList<InventoryMovementDto>> GetProductMovementHistoryAsync(
        long productId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the movement history for a specific warehouse
    /// </summary>
    Task<(IList<InventoryMovementDto> items, int totalCount)> GetWarehouseMovementHistoryAsync(
        int? warehouseId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchString = null,
        string? movementType = null,
        string? movementSubType = null,
        int page = 0,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending transfers for a warehouse (either as source or destination)
    /// </summary>
    Task<IList<InventoryMovementDto>> GetPendingTransfersAsync(
        int warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transfer history between warehouses
    /// </summary>
    Task<(IList<InventoryMovementDto> items, int totalCount)> GetTransferHistoryAsync(
        int? sourceWarehouseId = null,
        int? destinationWarehouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchString = null,
        int page = 0,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current alerts for stock levels
    /// </summary>
    Task<IList<InventoryAlertDto>> GetCurrentAlertsAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert history within a date range
    /// </summary>
    Task<IList<InventoryAlertDto>> GetAlertHistoryAsync(
        DateTime startDate,
        DateTime endDate,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the movement history for a specific warehouse filtered by movement types
    /// </summary>
    /// <param name="warehouseId"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="searchString"></param>
    /// <param name="movementTypes"></param>
    /// <param name="movementSubType"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    Task<(IList<InventoryMovementDto> items, int totalCount)> GetWarehouseMovementHistoryByTypesAsync(
        int? warehouseId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchString = null,
        string[]? movementTypes = null,
        string? movementSubType = null,
        int page = 0,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of current inventory alerts
    /// </summary>
    Task<int> GetCurrentAlertsCountAsync(
        CancellationToken cancellationToken = default);
}