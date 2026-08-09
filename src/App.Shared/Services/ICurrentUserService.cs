namespace App.Shared.Services;

public interface ICurrentUserService
{
    Task<string> GetUserIdAsync();
    Task<string?> GetUserNameAsync();
    Task<string?> GetFullNameAsync();
}
