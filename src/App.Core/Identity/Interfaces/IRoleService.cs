namespace App.Core.Identity.Interfaces;

public interface IRoleService
{
    Task<(bool Succeeded, string RoleId)> CreateRoleAsync(string roleName);
    Task<bool> DeleteRoleAsync(string roleId);
    Task<bool> UpdateRoleAsync(string roleId, string newName);
    Task<IList<string>> GetRoleClaimsAsync(string roleId);
    Task<bool> UpdateRoleClaimsAsync(string roleId, IEnumerable<string> claims);
    Task<IList<string>> GetAvailableRolesAsync();
}