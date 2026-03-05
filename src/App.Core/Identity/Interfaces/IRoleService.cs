namespace App.Core.Identity.Interfaces;

public interface IRoleService
{
    Task<(bool Succeeded, string RoleId)> CreateRoleAsync(string roleName);
    Task<bool> DeleteRoleAsync(string roleId);
    Task<bool> UpdateRoleAsync(string roleId, string newName);
    Task<IList<string>> GetRoleClaimsAsync(string roleName);
    Task<bool> UpdateRoleClaimsAsync(string roleName, IEnumerable<string> claims);
    Task<IList<string>> GetAvailableRolesAsync();
    Task<IList<(string Id, string Name, int UserCount)>> GetRolesWithDetailsAsync();
}