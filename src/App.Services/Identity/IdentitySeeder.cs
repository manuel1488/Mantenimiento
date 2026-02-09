using System.Security.Claims;

using App.Core.Constants;
using App.Core.Identity.Interfaces;
using App.Models.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace App.Services.Identity;

public class IdentitySeeder : IIdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedRolesAsync();
            await SeedSuperAdminAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during identity seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        // Get all roles defined in ApplicationRoles
        var roles = typeof(ApplicationRoles)
            .GetFields()
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        // Get all claims defined in ApplicationClaims
        var allClaims = ApplicationClaims.GetAllClaims().ToList();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole(roleName);
                await _roleManager.CreateAsync(role);
            }

            // Assign claims to role
            var roleClaims = GetClaimsForRole(roleName, allClaims);
            var currentClaims = await _roleManager.GetClaimsAsync(role);

            // Delete claims that are not in the new list
            foreach (var existingClaim in currentClaims)
            {
                if (!roleClaims.Contains(existingClaim.Value))
                {
                    await _roleManager.RemoveClaimAsync(role, existingClaim);
                }
            }

            // Add new claims
            foreach (var claim in roleClaims)
            {
                if (!currentClaims.Any(c => c.Value == claim))
                {
                    await _roleManager.AddClaimAsync(role, new Claim(claim, claim));
                }
            }
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        var superAdminUser = await _userManager.FindByNameAsync("admin");
        if (superAdminUser == null)
        {
            superAdminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                EmailConfirmed = true,
                FullName = "Super Admin",
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(superAdminUser, "Admin123!");
            if (result.Succeeded)
            {
                superAdminUser.ModifiedBy = "System";
                await _userManager.AddToRoleAsync(superAdminUser, ApplicationRoles.SuperAdmin);

                // Add all claims to SuperAdmin
                var allClaims = ApplicationClaims.GetAllClaims()
                    .Select(c => new Claim(c, c))
                    .ToList();

                await _userManager.AddClaimsAsync(superAdminUser, allClaims);
            }
        }
        else
        {
            // Update user claims
            var allClaims = ApplicationClaims.GetAllClaims()
                .Select(c => new Claim(c, c))
                .ToList();

            var currentClaims = await _userManager.GetClaimsAsync(superAdminUser);

            // Delete claims that are not in the new list
            foreach (var existingClaim in currentClaims)
            {
                if (!allClaims.Any(c => c.Value == existingClaim.Value))
                {
                    await _userManager.RemoveClaimAsync(superAdminUser, existingClaim);
                }
            }

            // Add new claims
            foreach (var claim in allClaims)
            {
                if (!currentClaims.Any(c => c.Value == claim.Value))
                {
                    await _userManager.AddClaimAsync(superAdminUser, claim);
                }
            }
        }
    }

    private IEnumerable<string> GetClaimsForRole(string roleName, List<string> allClaims)
    {
        return roleName switch
        {
            // SuperAdmin gets all claims
            ApplicationRoles.SuperAdmin => allClaims,

            // Other roles get specific claims
            ApplicationRoles.Admin => allClaims.Where(c =>
                c.StartsWith("Admin.") ||
                c.StartsWith("Shared.")),

            ApplicationRoles.ShopManager => allClaims.Where(c =>
                c.StartsWith("Shop.") ||
                c.StartsWith("Shared.")),

            ApplicationRoles.ShopEmployee => allClaims.Where(c =>
                (c.StartsWith("Shop.") && 
                !c.Contains(".Delete") && 
                !c.Contains(".Cancel") &&
                !c.EndsWith(".Manage")) ||
                (c.StartsWith("Shared.") && 
                !c.Contains(".Delete") && 
                !c.EndsWith(".Manage"))),

            _ => Enumerable.Empty<string>()
        };
    }
}