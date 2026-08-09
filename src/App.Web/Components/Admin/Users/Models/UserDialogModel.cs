namespace App.Web.Components.Admin.Users.Models;

public class UserDialogModel
{
    public string? Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Permissions { get; set; } = new List<string>();
}