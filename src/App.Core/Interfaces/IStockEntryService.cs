using App.Core.Common;
using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IStockEntryService
{
    Task<Result<StockEntryDto>> CreateStockEntryAsync(
        CreateStockEntryDto dto,
        CancellationToken ct = default);
}
