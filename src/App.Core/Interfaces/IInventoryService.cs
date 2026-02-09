using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IInventoryService
{
    Task<bool> ValidateStockAvailabilityAsync(
        long productId, 
        int warehouseId, 
        decimal quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inventory movement
    /// </summary>
    /// <param name="createDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MovementOperationResult> CreateMovementAsync(
        CreateInventoryMovementDto createDto,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new inventory transfer
    /// </summary>
    /// <param name="transferDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MovementOperationResult> CreateTransferAsync(
        CreateInventoryTransferDto transferDto,
        CancellationToken cancellationToken = default);

    Task<(int TotalCount, IList<InventoryMovementDto> Items)> GetMovementsAsync(
        int page = 1,
        int pageSize = 10,
        long? productId = null,
        int? warehouseId = null,
        string? movementType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IList<InventoryAlertDto>> GetStockAlertsAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates inventory settings like minimum and maximum stock levels
    /// </summary>
    Task<InventoryDto> UpdateInventorySettingsAsync(
        long productId, 
        int warehouseId, 
        UpdateInventoryDto updateDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates initial inventory for a product in a warehouse
    /// </summary>
    Task<InventoryOperationResult<InventoryMovementDto>> CreateInitialInventoryAsync(
        InitialInventoryLoadDto loadDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates initial inventory for multiple products in a warehouse
    /// </summary>
    Task<InventoryOperationResult<List<BulkInventoryLoadResultDto>>> CreateBulkInitialInventoryAsync(
        BulkInitialLoadRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an inventory adjustment setting the stock to a specific quantity
    /// </summary>
    // Cambio de firma para devolver MovementOperationResult en lugar de InventoryMovementDto
    Task<MovementOperationResult> CreateInventoryAdjustmentAsync(
        CreateInventoryAdjustmentDto adjustmentDto,
        CancellationToken cancellationToken = default);
}