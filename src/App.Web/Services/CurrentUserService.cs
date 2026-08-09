using System.Security.Claims;

using App.Models.Identity;
using App.Shared.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace App.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private const string USER_CACHE_KEY_PREFIX = "CurrentUser_";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

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
