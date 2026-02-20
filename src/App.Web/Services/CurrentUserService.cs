using System.Security.Claims;

using App.Core.Constants;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Shared.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace App.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private const string USER_CACHE_KEY_PREFIX = "CurrentUser_";
    private const string LOCATION_CACHE_KEY_PREFIX = "UserLocations_";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    private int? _activeLocationId;
    private bool _locationInitialized;

    public CurrentUserService(
        AuthenticationStateProvider? authenticationStateProvider = null,
        IServiceProvider? serviceProvider = null,
        IMemoryCache? cache = null)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public string UserId
    {
        get
        {
            if (_authenticationStateProvider == null)
                throw new InvalidOperationException("AuthenticationStateProvider is not initialized");

            try
            {
                var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
                var user = authState.User;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return userId ?? throw new InvalidOperationException("No user ID found");
            }
            catch
            {
                throw new InvalidOperationException("Unable to determine current user");
            }
        }
    }

    public string? UserName
    {
        get
        {
            if (_authenticationStateProvider == null)
            {
                return null;
            }

            try
            {
                var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
                return authState.User.Identity?.Name;
            }
            catch
            {
                return null;
            }
        }
    }

    public string FullName
    {
        get
        {
            return GetCurrentUser()?.FullName ??
                throw new InvalidOperationException("Unable to determine user's full name");
        }
    }

    public int? ActiveLocationId
    {
        get
        {
            if (!_locationInitialized)
            {
                InitializeActiveLocationAsync().GetAwaiter().GetResult();
            }
            return _activeLocationId;
        }
    }

    public bool IsGlobalAccess
    {
        get
        {
            if (_authenticationStateProvider == null)
                return false;

            try
            {
                var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
                var user = authState.User;
                return user.IsInRole(ApplicationRoles.SuperAdmin) ||
                       user.IsInRole(ApplicationRoles.Admin);
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<IReadOnlyList<int>> GetAssignedLocationIdsAsync()
    {
        try
        {
            var userId = UserId;
            var cacheKey = $"{LOCATION_CACHE_KEY_PREFIX}{userId}";

            var locationIds = _cache.Get<IReadOnlyList<int>>(cacheKey);
            if (locationIds != null)
                return locationIds;

            using var scope = _serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();

            var ids = await context.UserLocations
                .AsNoTracking()
                .Where(ul => ul.UserId == userId)
                .Select(ul => ul.LocationId)
                .ToListAsync();

            _cache.Set(cacheKey, (IReadOnlyList<int>)ids, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CACHE_DURATION,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return ids;
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    public Task EnsureInitializedAsync() => InitializeActiveLocationAsync();

    public async Task SetActiveLocationAsync(int? locationId)
    {
        if (locationId.HasValue)
        {
            var hasAccess = await HasAccessToLocationAsync(locationId.Value);
            if (!hasAccess)
                return;
        }

        _activeLocationId = locationId;
    }

    public async Task<bool> HasAccessToLocationAsync(int locationId)
    {
        if (IsGlobalAccess)
            return true;

        var assignedIds = await GetAssignedLocationIdsAsync();
        return assignedIds.Contains(locationId);
    }

    private async Task InitializeActiveLocationAsync()
    {
        if (_locationInitialized)
            return;

        _locationInitialized = true;

        try
        {
            // Get user's assigned locations
            var assignedLocationIds = await GetAssignedLocationIdsAsync();

            if (assignedLocationIds.Count == 1)
            {
                // User has exactly one location assigned - use it automatically
                _activeLocationId = assignedLocationIds.First();
            }
            else if (assignedLocationIds.Count > 1)
            {
                // User has multiple locations assigned - use the first one
                // This can be enhanced later to remember user's last selected location
                _activeLocationId = assignedLocationIds.First();
            }
            else
            {
                // User has no locations assigned
                // For global access users (admins), leave it null so they can see all data
                // For regular users, this is an error condition
                _activeLocationId = null;
            }
        }
        catch
        {
            _activeLocationId = null;
        }
    }

    private ApplicationUser? GetCurrentUser()
    {
        try
        {
            var userId = UserId;
            if (userId == "System")
                return null;

            var cacheKey = $"{USER_CACHE_KEY_PREFIX}{userId}";

            var value = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = CACHE_DURATION;
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                return userManager.FindByIdAsync(userId).Result;
            });

            return value;
        }
        catch
        {
            return null;
        }
    }
}
