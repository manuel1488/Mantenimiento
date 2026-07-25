using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IPhysicalInventoryCountService
{
    /// <summary>
    /// Loads every actively-tracked product at the location with its current system quantity,
    /// used to pre-populate the count sheet.
    /// </summary>
    Task<WarehouseStockDto> GetCountSheetAsync(int locationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a physical count atomically: only lines whose counted quantity differs from the
    /// system quantity generate an inventory movement; unchanged lines are still reported for the record.
    /// </summary>
    Task<InventoryOperationResult<PhysicalInventoryCountResultDto>> CreateAndApplyAsync(
        CreatePhysicalInventoryCountDto dto,
        CancellationToken cancellationToken = default);

    Task<(int TotalCount, IList<PhysicalInventoryCountResultDto> Items)> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        int? locationId = null,
        CancellationToken cancellationToken = default);

    Task<PhysicalInventoryCountResultDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<byte[]> GeneratePdfAsync(Guid batchId, CancellationToken cancellationToken = default);
}
