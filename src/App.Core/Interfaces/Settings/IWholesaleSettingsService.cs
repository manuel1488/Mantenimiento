using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces.Settings;

public interface IWholesaleSettingsService
{
    Task<Result<WholesaleSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<Result<WholesaleSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateWholesaleSettingsDto updateDto,
        CancellationToken cancellationToken = default);
}
