using System.Security.Claims;

using App.Models.Identity;
using App.Shared.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace App.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private const string USER_CACHE_KEY_PREFIX = "CurrentUser_";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

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