using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces.Settings;

public interface ILabelSettingsService
{
    Task<Result<LabelSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<Result<LabelSettingsDto>> CreateOrUpdateSettingsAsync(UpdateLabelSettingsDto dto, CancellationToken cancellationToken = default);
}
