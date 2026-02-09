using App.Core.Common;
using App.Core.DTOs.Identity;

using Microsoft.AspNetCore.Identity;

namespace App.Core.Identity.Interfaces;

public interface IIdentityService
{
    /// <summary>
    /// Gets a paginated list of users with their roles
    /// </summary>
    Task<(int TotalCount, IList<UserDto> Users)> GetUsersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? role = null,
        bool? isActive = null);

    /// <summary>
    /// Creates a new user with the specified properties
    /// </summary>
    Task<(IdentityResult Result, string UserId)> CreateUserAsync(CreateUserDto createUserDto);

    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);

    /// <summary>
    /// Soft deletes a user
    /// </summary>
    Task<IdentityResult> DeleteUserAsync(string userId);

    /// <summary>
    /// Gets the roles assigned to a user
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Assigns roles to a user
    /// </summary>
    Task<IdentityResult> AddUserToRolesAsync(string userId, IEnumerable<string> roles);

    /// <summary>
    /// Removes roles from a user
    /// </summary>
    Task<IdentityResult> RemoveUserFromRolesAsync(string userId, IEnumerable<string> roles);

    /// <summary>
    /// Changes a user's password
    /// </summary>
    Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);

    Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName);

    Task<Dictionary<string, IEnumerable<string>>> GetAllRolesWithPermissionsAsync();

    Task UpdateUserPermissionsAsync(string userId, IEnumerable<string> permissions);

    /// <summary>
    /// Generates a password reset token for the user with the specified email
    /// </summary>
    Task<(bool Succeeded, string? Error)> ForgotPasswordAsync(string email);

    /// <summary>
    /// Resets a user's password using a reset token
    /// </summary>
    Task<(bool Succeeded, string? Error)> ResetPasswordAsync(ResetPasswordDto resetDto);

    /// <summary>
    /// Resets a user's password directly (admin functionality)
    /// </summary>
    Task<(bool Succeeded, string? Error)> AdminResetPasswordAsync(string userId, string newPassword);


    /// <summary>
    /// Obtiene un usuario por su ID
    /// </summary>
    /// <param name="userId">El ID del usuario</param>
    /// <returns>El usuario encontrado o null si no existe</returns>
    Task<Result<UserDto>> GetUserByIdAsync(string userId);
}