using App.Core.Common;
using App.Core.DTOs.Location;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Identity;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Identity;

public class UserLocationService : IUserLocationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<UserLocationService> _logger;
    private readonly IStringLocalizer<UserLocationService> _localizer;
    private readonly IMemoryCache _cache;
    private const string LOCATION_CACHE_KEY_PREFIX = "UserLocations_";

    public UserLocationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<UserLocationService> logger,
        IStringLocalizer<UserLocationService> localizer,
        IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _cache = cache;
    }

    public async Task<Result<IList<LocationDto>>> GetUserLocationsAsync(string userId, LocationType? type = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.UserLocations
                .AsNoTracking()
                .Include(ul => ul.Location)
                .Where(ul => ul.UserId == userId && ul.Location.IsActive);

            if (type.HasValue)
            {
                query = query.Where(ul => ul.Location.Type == type.Value);
            }

            var userLocations = await query
                .Select(ul => ul.Location)
                .ToListAsync();

            var locationDtos = _mapper.Map<IList<LocationDto>>(userLocations);
            return Result<IList<LocationDto>>.Success(locationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user locations for user {UserId}", userId);
            return Result<IList<LocationDto>>.Failure(_localizer["Error retrieving user locations"]);
        }
    }

    public async Task<Result> AssignLocationsToUserAsync(string userId, IList<int> locationIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Verify user exists
            var userExists = await context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Result.Failure(_localizer["User not found"]);
            }

            // Verify all locations exist
            var existingLocationIds = await context.Locations
                .Where(l => locationIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync();

            var missingLocationIds = locationIds.Except(existingLocationIds).ToList();
            if (missingLocationIds.Any())
            {
                return Result.Failure(_localizer["One or more locations not found"]);
            }

            // Get current user locations
            var currentUserLocations = await context.UserLocations
                .Where(ul => ul.UserId == userId)
                .ToListAsync();

            // Remove locations that are no longer assigned
            var locationsToRemove = currentUserLocations
                .Where(ul => !locationIds.Contains(ul.LocationId))
                .ToList();

            if (locationsToRemove.Any())
            {
                context.UserLocations.RemoveRange(locationsToRemove);
            }

            // Add new location assignments
            var currentLocationIds = currentUserLocations.Select(ul => ul.LocationId).ToList();
            var locationsToAdd = locationIds
                .Except(currentLocationIds)
                .Select(locationId => new UserLocation
                {
                    UserId = userId,
                    LocationId = locationId
                })
                .ToList();

            if (locationsToAdd.Any())
            {
                await context.UserLocations.AddRangeAsync(locationsToAdd);
            }

            await context.SaveChangesAsync();
            _cache.Remove($"{LOCATION_CACHE_KEY_PREFIX}{userId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning locations to user {UserId}", userId);
            return Result.Failure(_localizer["Error assigning locations to user"]);
        }
    }

    public async Task<Result> RemoveLocationFromUserAsync(string userId, int locationId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var userLocation = await context.UserLocations
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LocationId == locationId);

            if (userLocation == null)
            {
                return Result.Failure(_localizer["User location assignment not found"]);
            }

            context.UserLocations.Remove(userLocation);
            await context.SaveChangesAsync();
            _cache.Remove($"{LOCATION_CACHE_KEY_PREFIX}{userId}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing location {LocationId} from user {UserId}", locationId, userId);
            return Result.Failure(_localizer["Error removing location from user"]);
        }
    }
}
