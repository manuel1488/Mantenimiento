namespace App.Core.Identity.Models;

public class AuthenticationResult
{
    public bool Succeeded { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public IList<string>? Roles { get; set; }
    public IList<string>? Claims { get; set; }
    public List<string> Errors { get; set; } = new();
}