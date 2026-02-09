using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace App.Web.Services;

/// <summary>
/// Implementation of IPermissionService using AuthorizationService
/// </summary>
public class PermissionCheckService : IPermissionCheckService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public PermissionCheckService(
        IAuthorizationService authorizationService,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _authorizationService = authorizationService;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(string permission)
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        return (await _authorizationService.AuthorizeAsync(user, permission)).Succeeded;
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyPermissionAsync(params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (await HasPermissionAsync(permission))
            {
                return true;
            }
        }
        
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> HasAllPermissionsAsync(params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (!await HasPermissionAsync(permission))
            {
                return false;
            }
        }
        
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> HasReadAccessAsync(string viewPermission, string managePermission)
    {
        // User has read access if they have either view or manage permission
        return await HasAnyPermissionAsync(viewPermission, managePermission);
    }

    /// <inheritdoc />
    public async Task<bool> HasWriteAccessAsync(string managePermission)
    {
        // User has write access only if they have manage permission
        return await HasPermissionAsync(managePermission);
    }
}