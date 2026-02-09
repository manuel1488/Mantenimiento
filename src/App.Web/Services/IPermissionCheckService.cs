using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace App.Web.Services;

/// <summary>
/// Service for checking user permissions and authorization rules
/// </summary>
public interface IPermissionCheckService
{
    /// <summary>
    /// Checks if the current user has the specified permission
    /// </summary>
    Task<bool> HasPermissionAsync(string permission);
    
    /// <summary>
    /// Checks if the current user has any of the specified permissions
    /// </summary>
    Task<bool> HasAnyPermissionAsync(params string[] permissions);
    
    /// <summary>
    /// Checks if the current user has all of the specified permissions
    /// </summary>
    Task<bool> HasAllPermissionsAsync(params string[] permissions);
    
    /// <summary>
    /// Checks if the current user has read access based on view and manage permissions
    /// </summary>
    Task<bool> HasReadAccessAsync(string viewPermission, string managePermission);
    
    /// <summary>
    /// Checks if the current user has write access based on manage permission
    /// </summary>
    Task<bool> HasWriteAccessAsync(string managePermission);
}