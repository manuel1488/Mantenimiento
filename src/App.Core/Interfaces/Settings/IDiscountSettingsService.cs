using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces.Settings;

public interface IDiscountSettingsService
{
    /// <summary>
    /// Gets the current discount settings
    /// </summary>
    Task<Result<DiscountSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the discount settings
    /// </summary>
    Task<Result<DiscountSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateDiscountSettingsDto updateDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a discount percentage is allowed
    /// </summary>
    Task<Result<bool>> ValidateDiscountAsync(
        decimal discountPercentage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a discount requires authorization based on the settings
    /// </summary>
    Task<Result<bool>> RequiresAuthorizationAsync(
        decimal discountPercentage,
        CancellationToken cancellationToken = default);
}
