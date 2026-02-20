using App.Core.Common;
using App.Core.DTOs.Location;

namespace App.Core.Interfaces;

public interface ILocationTicketSettingsService
{
    /// <summary>
    /// Gets ticket settings for a specific location
    /// Falls back to global settings if no location-specific settings exist
    /// </summary>
    Task<Result<LocationTicketSettingsDto>> GetByLocationIdAsync(int locationId);

    /// <summary>
    /// Gets ticket settings by ID
    /// </summary>
    Task<Result<LocationTicketSettingsDto>> GetByIdAsync(int id);

    /// <summary>
    /// Creates ticket settings for a location
    /// </summary>
    Task<Result<LocationTicketSettingsDto>> CreateAsync(CreateLocationTicketSettingsDto createDto);

    /// <summary>
    /// Updates ticket settings for a location
    /// </summary>
    Task<Result<LocationTicketSettingsDto>> UpdateAsync(int id, UpdateLocationTicketSettingsDto updateDto);

    /// <summary>
    /// Deletes ticket settings for a location (will fall back to global settings)
    /// </summary>
    Task<Result> DeleteAsync(int id);

    /// <summary>
    /// Copies global ticket settings to a specific location as starting point
    /// </summary>
    Task<Result<LocationTicketSettingsDto>> CopyFromGlobalAsync(int locationId);

    /// <summary>
    /// Checks if a location has specific ticket settings
    /// </summary>
    Task<bool> HasSettingsAsync(int locationId);
}
