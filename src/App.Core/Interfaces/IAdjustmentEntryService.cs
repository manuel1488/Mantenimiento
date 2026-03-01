using App.Core.Common;
using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

public interface IAdjustmentEntryService
{
    Task<Result<AdjustmentEntryDto>> CreateAdjustmentEntryAsync(
        CreateAdjustmentEntryDto dto,
        CancellationToken ct = default);
}
