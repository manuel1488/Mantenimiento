using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces.Settings;

public interface IInventorySettingsService
{
    /// <summary>
    /// Gets the current inventory settings
    /// </summary>
    Task<Result<InventorySettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the inventory settings
    /// </summary>
    Task<Result<InventorySettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateInventorySettingsDto updateDto,
        CancellationToken cancellationToken = default);
}
