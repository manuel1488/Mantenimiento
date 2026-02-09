namespace App.Core.Interfaces;

public interface IDiscountAuthorizerService
{
    Task<bool> CanUserAuthorizeDiscountsAsync(string userId);
    Task<(bool Success, bool InvalidCredentials, string? UserId, string? UserName)> ValidateAuthorizerCredentialsAsync(
        string username, string password);
}