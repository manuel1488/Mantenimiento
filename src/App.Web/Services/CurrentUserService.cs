using System.Security.Claims;

using App.Core.Constants;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Shared.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace App.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
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
        IMemoryCache? cache = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<string> GetUserIdAsync()
    {
        // Primary: HttpContext (works for both API controllers and Blazor initial render) — synchronous, no await needed.
        var httpUserId = _httpContextAccessor?.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (httpUserId != null)
            return httpUserId;

        // Fallback: AuthenticationStateProvider (Blazor async circuit)
        if (_authenticationStateProvider == null)
            throw new InvalidOperationException("AuthenticationStateProvider is not initialized");

        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId ?? throw new InvalidOperationException("No user ID found");
        }
        catch
        {
            throw new InvalidOperationException("Unable to determine current user");
        }
    }

    public async Task<string?> GetUserNameAsync()
    {
        if (_authenticationStateProvider == null)
            return null;

        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.Name;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetFullNameAsync()
    {
        var user = await GetCurrentUserAsync();
        return user?.FullName ?? await GetUserNameAsync();
    }

    public async Task<int?> GetActiveLocationIdAsync()
    {
        if (!_locationInitialized)
        {
            await InitializeActiveLocationAsync();
        }
        return _activeLocationId;
    }

    public async Task<bool> GetIsGlobalAccessAsync()
    {
        if (_authenticationStateProvider == null)
            return false;

        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            return user.IsInRole(ApplicationRoles.SuperAdmin) ||
                   user.IsInRole(ApplicationRoles.Admin);
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<int>> GetAssignedLocationIdsAsync()
    {
        try
        {
            var userId = await GetUserIdAsync();
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

    public async Task EnsureInitializedAsync()
    {
        // If previously initialized but the location cache was invalidated externally
        // (e.g., admin updated user's locations), reset so we pick up fresh data.
        if (_locationInitialized)
        {
            try
            {
                var userId = await GetUserIdAsync();
                var cacheKey = $"{LOCATION_CACHE_KEY_PREFIX}{userId}";
                if (!_cache.TryGetValue(cacheKey, out _))
                {
                    _locationInitialized = false;
                    _activeLocationId = null;
                }
            }
            catch
            {
                // If we can't get userId, leave as-is
            }
        }

        await InitializeActiveLocationAsync();
    }

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
        if (await GetIsGlobalAccessAsync())
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

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        try
        {
            var userId = await GetUserIdAsync();
            if (userId == "System")
                return null;

            var cacheKey = $"{USER_CACHE_KEY_PREFIX}{userId}";

            if (_cache.TryGetValue(cacheKey, out ApplicationUser? cached))
                return cached;

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var value = await userManager.FindByIdAsync(userId);

            _cache.Set(cacheKey, value, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CACHE_DURATION,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)

            });

            return value;
        }
        catch
        {
            return null;
        }
    }
}
