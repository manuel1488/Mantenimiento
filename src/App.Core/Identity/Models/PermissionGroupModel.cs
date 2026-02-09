namespace App.Core.Identity.Models;

public class PermissionGroupModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<PermissionModel> Permissions { get; set; } = Array.Empty<PermissionModel>();
}