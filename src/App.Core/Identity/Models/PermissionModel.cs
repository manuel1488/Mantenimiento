namespace App.Core.Identity.Models;

public class PermissionModel
{
    public string Name { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}