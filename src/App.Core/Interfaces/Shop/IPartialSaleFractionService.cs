using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

/// <summary>
/// Service for managing partial sale fractions (1/2, 1/4, 1/8, etc.).
/// </summary>
public interface IPartialSaleFractionService
{
    /// <summary>
    /// Gets all active partial sale fractions.
    /// </summary>
    Task<Result<IList<PartialSaleFractionDto>>> GetActiveFractionsAsync();

    /// <summary>
    /// Gets all partial sale fractions including inactive ones.
    /// </summary>
    Task<Result<IList<PartialSaleFractionDto>>> GetAllFractionsAsync();

    /// <summary>
    /// Gets a fraction by ID.
    /// </summary>
    Task<Result<PartialSaleFractionDto>> GetFractionByIdAsync(int id);

    /// <summary>
    /// Gets a fraction by code (e.g., "1/2", "1/4").
    /// </summary>
    Task<Result<PartialSaleFractionDto>> GetFractionByCodeAsync(string code);

    /// <summary>
    /// Creates a new partial sale fraction.
    /// </summary>
    Task<Result<PartialSaleFractionDto>> CreateFractionAsync(CreatePartialSaleFractionDto dto);

    /// <summary>
    /// Updates an existing partial sale fraction.
    /// </summary>
    Task<Result<PartialSaleFractionDto>> UpdateFractionAsync(int id, UpdatePartialSaleFractionDto dto);

    /// <summary>
    /// Deletes a partial sale fraction (soft delete).
    /// </summary>
    Task<Result> DeleteFractionAsync(int id);

    /// <summary>
    /// Toggles the active status of a fraction.
    /// </summary>
    Task<Result> ToggleActiveAsync(int id);

    /// <summary>
    /// Validates if a code is unique.
    /// </summary>
    Task<Result<bool>> ValidateUniqueCodeAsync(string code, int? excludeId = null);
}
