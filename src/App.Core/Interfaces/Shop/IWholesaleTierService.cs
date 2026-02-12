using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

/// <summary>
/// Service for managing wholesale pricing tiers.
/// </summary>
public interface IWholesaleTierService
{
    /// <summary>
    /// Gets all active wholesale tiers.
    /// </summary>
    Task<Result<IList<WholesaleTierDto>>> GetActiveTiersAsync();

    /// <summary>
    /// Gets all wholesale tiers including inactive ones.
    /// </summary>
    Task<Result<IList<WholesaleTierDto>>> GetAllTiersAsync();

    /// <summary>
    /// Gets a tier by ID.
    /// </summary>
    Task<Result<WholesaleTierDto>> GetTierByIdAsync(int id);

    /// <summary>
    /// Creates a new wholesale tier.
    /// </summary>
    Task<Result<WholesaleTierDto>> CreateTierAsync(CreateWholesaleTierDto dto);

    /// <summary>
    /// Updates an existing wholesale tier.
    /// </summary>
    Task<Result<WholesaleTierDto>> UpdateTierAsync(int id, UpdateWholesaleTierDto dto);

    /// <summary>
    /// Deletes a wholesale tier (soft delete).
    /// </summary>
    Task<Result> DeleteTierAsync(int id);

    /// <summary>
    /// Toggles the active status of a tier.
    /// </summary>
    Task<Result> ToggleActiveAsync(int id);

    /// <summary>
    /// Validates if a name is unique.
    /// </summary>
    Task<Result<bool>> ValidateUniqueNameAsync(string name, int? excludeId = null);
}
