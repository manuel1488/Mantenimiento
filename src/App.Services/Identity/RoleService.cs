using System.Security.Claims;

using App.Core.Identity.Interfaces;
using App.Models.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace App.Services.Identity;

public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<RoleService> _localizer;

    public RoleService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<RoleService> localizer)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _localizer = localizer;
    }

    public async Task<(bool Succeeded, string RoleId)> CreateRoleAsync(string roleName)
    {
        var role = new IdentityRole(roleName);
        var result = await _roleManager.CreateAsync(role);
        return (result.Succeeded, role.Id);
    }

    public async Task<bool> DeleteRoleAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return false;

        var result = await _roleManager.DeleteAsync(role);
        return result.Succeeded;
    }

    public async Task<bool> UpdateRoleAsync(string roleId, string newName)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return false;

        role.Name = newName;
        var result = await _roleManager.UpdateAsync(role);
        return result.Succeeded;
    }

    public async Task<IList<string>> GetRoleClaimsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return Array.Empty<string>();

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims.Select(c => c.Value).ToList();
    }

    public async Task<bool> UpdateRoleClaimsAsync(string roleName, IEnumerable<string> claims)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        var currentClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in currentClaims)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var claim in claims)
        {
            var result = await _roleManager.AddClaimAsync(role, new Claim(claim, claim));
            if (!result.Succeeded) return false;
        }

        return true;
    }

    public async Task<IList<string>> GetAvailableRolesAsync()
    {
        return await _roleManager.Roles
            .Where(r => r.Name != null)
            .Select(r => r.Name!)
            .ToListAsync();
    }

    public async Task<IList<(string Id, string Name, int UserCount)>> GetRolesWithDetailsAsync()
    {
        var roles = await _roleManager.Roles
            .Where(r => r.Name != null)
            .ToListAsync();

        var result = new List<(string Id, string Name, int UserCount)>();
        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            result.Add((role.Id, role.Name!, users.Count));
        }

        return result;
    }
}
