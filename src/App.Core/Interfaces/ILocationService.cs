using App.Core.DTOs.Location;
using App.Core.Enums.Shop;

namespace App.Core.Interfaces;

public interface ILocationService
{
    /// <summary>
    /// Gets a paginated list of locations
    /// </summary>
    Task<(int TotalCount, IList<LocationDto> Items)> GetLocationsAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null,
        LocationType? type = null);

    /// <summary>
    /// Gets all active locations
    /// </summary>
    Task<IList<LocationDto>> GetActiveLocationsAsync(LocationType? type = null);

    /// <summary>
    /// Gets a location by ID
    /// </summary>
    Task<LocationDto?> GetLocationByIdAsync(int id);

    /// <summary>
    /// Creates a new location
    /// </summary>
    Task<LocationDto> CreateLocationAsync(CreateLocationDto createDto);

    /// <summary>
    /// Updates an existing location
    /// </summary>
    Task<LocationDto> UpdateLocationAsync(int id, UpdateLocationDto updateDto);

    /// <summary>
    /// Soft deletes a location
    /// </summary>
    Task<bool> DeleteLocationAsync(int id);

    /// <summary>
    /// Validates that a location name is unique
    /// </summary>
    Task<bool> ValidateUniqueNameAsync(string name, int? excludeId = null);
}
