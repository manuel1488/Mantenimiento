using Microsoft.Extensions.Localization;

namespace App.Core.Interfaces.Identity;

public interface IPasswordValidationService
{
    string? ValidatePassword(string password, IStringLocalizer localizer);
    string? ValidatePasswordMatch(string password, string confirmPassword, IStringLocalizer localizer);
}