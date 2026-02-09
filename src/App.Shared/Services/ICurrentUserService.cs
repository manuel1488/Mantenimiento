namespace App.Shared.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string? UserName { get; }
    string FullName { get; }
}