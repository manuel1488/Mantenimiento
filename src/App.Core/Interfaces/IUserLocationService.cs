using App.Core.Common;
using App.Core.DTOs.Location;
using App.Core.Enums.Shop;

namespace App.Core.Interfaces;

public interface IUserLocationService
{
    Task<Result<IList<LocationDto>>> GetUserLocationsAsync(string userId, LocationType? type = null);

    Task<Result> AssignLocationsToUserAsync(string userId, IList<int> locationIds);

    Task<Result> RemoveLocationFromUserAsync(string userId, int locationId);
}
