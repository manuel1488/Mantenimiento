using System.Security.Claims;
using App.Core.Constants;
using App.Core.Interfaces;
using App.Models.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace App.Services;

public class DiscountAuthorizerService : IDiscountAuthorizerService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DiscountAuthorizerService> _logger;

    public DiscountAuthorizerService(
        UserManager<ApplicationUser> userManager,
        ILogger<DiscountAuthorizerService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> CanUserAuthorizeDiscountsAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            // Verificar si el usuario tiene el claim de autorización de descuentos
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.Any(c => c.Type == ApplicationClaims.Shop.AuthorizeDiscounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can authorize discounts");
            return false;
        }
    }

    public async Task<(bool Success, bool InvalidCredentials, string? UserId, string? UserName)> ValidateAuthorizerCredentialsAsync(
        string username, string password)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || !user.IsActive)
            {
                return (false, true, null, null); // InvalidCredentials = true
            }

            // Verificar credenciales
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                return (false, true, null, null); // InvalidCredentials = true
            }

            // Verificar si puede autorizar descuentos mediante el claim
            var claims = await _userManager.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == ApplicationClaims.Shop.AuthorizeDiscounts))
            {
                return (false, false, null, null); // Credenciales correctas pero sin permiso
            }

            return (true, false, user.Id, user.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authorizer credentials");
            return (false, false, null, null);
        }
    }
}