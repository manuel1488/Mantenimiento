namespace App.Core.DTOs.Identity;

/// <summary>
/// Data transfer object for user update
/// </summary>
public class UpdateUserDto
{
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public bool HasGlobalLocationAccess { get; set; }
}