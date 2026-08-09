namespace App.Core.DTOs.Identity;

/// <summary>
/// Data transfer object for user creation
/// </summary>
public class CreateUserDto
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Role { get; set; }
}