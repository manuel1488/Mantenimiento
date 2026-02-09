using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IBulkLoadResultsExporter
{
    Task<byte[]> ExportAsync(List<BulkInventoryLoadResultDto> results, CancellationToken cancellationToken = default);
}