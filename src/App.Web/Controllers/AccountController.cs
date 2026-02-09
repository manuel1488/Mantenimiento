using App.Models.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login()
    {
        var form = await Request.ReadFormAsync();

        var userName = form["Input.UserName"].ToString();
        var password = form["Input.Password"].ToString();
        var rememberMe = form["Input.RememberMe"] == "True";

        // Validación básica
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            return Redirect("/Account/Login?error=invalid");
        }

        // Lógica de autenticación
        var user = await _signInManager.UserManager.FindByNameAsync(userName);
        if (user == null || !user.IsActive)
        {
            return Redirect("/Account/Login?error=invalid");
        }

        var result = await _signInManager.PasswordSignInAsync(
            userName,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLogin = DateTime.UtcNow;
            await _signInManager.UserManager.UpdateAsync(user);
            _logger.LogInformation("User {UserName} logged in successfully", userName);

            // Siempre redirigir a la raíz después de una autenticación exitosa
            return LocalRedirect("/");
        }

        return Redirect("/Account/Login?error=failed");
    }

    
    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword()
    {
        try
        {
            var form = await Request.ReadFormAsync();

            var email = form["Email"].ToString();
            var token = form["Token"].ToString();
            var newPassword = form["Input.NewPassword"].ToString();
            var confirmPassword = form["Input.ConfirmPassword"].ToString();

            // Validación básica
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Reset password attempted with invalid token or email");
                return Redirect("/Account/ResetPassword?status=invalid");
            }

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                _logger.LogWarning("Reset password attempted with empty password for email: {Email}", email);
                return Redirect($"/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}&error=empty");
            }

            if (newPassword != confirmPassword)
            {
                _logger.LogWarning("Reset password attempted with mismatched passwords for email: {Email}", email);
                return Redirect($"/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}&error=mismatch");
            }

            // Lógica de restablecimiento de contraseña
            var user = await _signInManager.UserManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Reset password attempted for non-existent user: {Email}", email);
                return Redirect("/Account/ResetPassword?status=invalid");
            }

            var result = await _signInManager.UserManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user: {Email}", email);
                return Redirect("/Account/ResetPassword?status=success");
            }
            else
            {
                _logger.LogWarning("Password reset failed for user: {Email}. Errors: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));

                // Si el token es inválido o ha expirado, enviar a página de error de token
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    return Redirect("/Account/ResetPassword?status=invalid");
                }

                // Para otros errores, volver al formulario con mensaje de error general
                return Redirect($"/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}&error=failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset");
            return Redirect("/Account/ResetPassword?error=exception");
        }
    }
}