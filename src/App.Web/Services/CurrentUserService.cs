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
    private const string BRANCH_CACHE_KEY_PREFIX = "UserBranches_";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    private int? _activeBranchId;
    private bool _branchInitialized;

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

    public int? ActiveBranchId
    {
        get
        {
            if (!_branchInitialized)
            {
                InitializeActiveBranchAsync().GetAwaiter().GetResult();
            }
            return _activeBranchId;
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

    public async Task<IReadOnlyList<int>> GetAssignedBranchIdsAsync()
    {
        try
        {
            var userId = UserId;
            var cacheKey = $"{BRANCH_CACHE_KEY_PREFIX}{userId}";

            var branchIds = _cache.Get<IReadOnlyList<int>>(cacheKey);
            if (branchIds != null)
                return branchIds;

            using var scope = _serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();

            var ids = await context.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BranchId)
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

    public async Task SetActiveBranchAsync(int? branchId)
    {
        if (branchId.HasValue)
        {
            var hasAccess = await HasAccessToBranchAsync(branchId.Value);
            if (!hasAccess)
                return;
        }

        _activeBranchId = branchId;
    }

    public async Task<bool> HasAccessToBranchAsync(int branchId)
    {
        if (IsGlobalAccess)
            return true;

        var assignedIds = await GetAssignedBranchIdsAsync();
        return assignedIds.Contains(branchId);
    }

    private async Task InitializeActiveBranchAsync()
    {
        if (_branchInitialized)
            return;

        _branchInitialized = true;

        try
        {
            // Get user's assigned branches
            var assignedBranchIds = await GetAssignedBranchIdsAsync();

            if (assignedBranchIds.Count == 1)
            {
                // User has exactly one branch assigned - use it automatically
                _activeBranchId = assignedBranchIds.First();
            }
            else if (assignedBranchIds.Count > 1)
            {
                // User has multiple branches assigned - use the first one
                // This can be enhanced later to remember user's last selected branch
                _activeBranchId = assignedBranchIds.First();
            }
            else
            {
                // User has no branches assigned
                // For global access users (admins), leave it null so they can see all data
                // For regular users, this is an error condition
                _activeBranchId = null;
            }
        }
        catch
        {
            _activeBranchId = null;
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
