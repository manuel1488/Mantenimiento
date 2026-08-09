namespace App.Core.DTOs.Identity;

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}