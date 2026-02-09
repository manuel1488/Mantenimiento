using Microsoft.Extensions.Localization;
using App.Core.Constants;

namespace App.Web.Services;

public class RoleTranslationService
{
    private readonly IStringLocalizer<RoleTranslationService> L;

    public RoleTranslationService(IStringLocalizer<RoleTranslationService> localizer)
    {
        L = localizer;
    }

    /// <summary>
    /// Gets the display name for a role
    /// </summary>
    /// <param name="roleName">The role name</param>
    /// <returns>The translated display name</returns>
    public string GetRoleDisplayName(string roleName) => roleName switch
    {
        ApplicationRoles.SuperAdmin => L["Role.SuperAdmin"],
        ApplicationRoles.Admin => L["Role.Admin"],
        ApplicationRoles.ShopManager => L["Role.ShopManager"],
        ApplicationRoles.ShopEmployee => L["Role.ShopEmployee"],
        _ => roleName
    };

    /// <summary>
    /// Gets the description for a role
    /// </summary>
    /// <param name="roleName">The role name</param>
    /// <returns>The translated description</returns>
    public string GetRoleDescription(string roleName) => roleName switch
    {
        ApplicationRoles.SuperAdmin => L["Role.SuperAdmin.Description"],
        ApplicationRoles.Admin => L["Role.Admin.Description"],
        ApplicationRoles.ShopManager => L["Role.ShopManager.Description"],
        ApplicationRoles.ShopEmployee => L["Role.ShopEmployee.Description"],
        _ => L["Role.NoDescription"]
    };
}