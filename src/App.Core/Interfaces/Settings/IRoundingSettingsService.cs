using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces.Settings;

public interface IRoundingSettingsService
{
    /// <summary>
    /// Gets the current rounding settings
    /// </summary>
    Task<Result<RoundingSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates the rounding settings
    /// </summary>
    Task<Result<RoundingSettingsDto>> CreateOrUpdateSettingsAsync(
        UpdateRoundingSettingsDto updateDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the rounding adjustment for a given amount
    /// Returns 0 if rounding is disabled or amount is below threshold
    /// </summary>
    Task<Result<decimal>> CalculateRoundingAsync(
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies rounding to an amount and returns the rounded total and rounding amount
    /// </summary>
    Task<Result<(decimal RoundedTotal, decimal RoundingAmount)>> ApplyRoundingAsync(
        decimal amount,
        CancellationToken cancellationToken = default);
}
