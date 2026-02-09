
using App.Core.Interfaces.Identity;

using Microsoft.Extensions.Localization;

namespace App.Core.Services;

public class PasswordValidationService : IPasswordValidationService
{
    public string? ValidatePassword(string password, IStringLocalizer localizer)
    {
        if (string.IsNullOrWhiteSpace(password))
            return localizer["PasswordRequired"];

        if (password.Length < 8)
            return localizer["PasswordMin8Chars"];

        if (!password.Any(char.IsUpper))
            return localizer["PasswordRequiresUppercase"];

        if (!password.Any(char.IsLower))
            return localizer["PasswordRequiresLowercase"];

        if (!password.Any(char.IsDigit))
            return localizer["PasswordRequiresDigit"];

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return localizer["PasswordRequiresSpecial"];

        return null;
    }

    public string? ValidatePasswordMatch(string password, string confirmPassword, IStringLocalizer localizer)
    {
        if (string.IsNullOrWhiteSpace(confirmPassword))
            return localizer["ConfirmPasswordRequired"];

        if (confirmPassword != password)
            return localizer["PasswordsDoNotMatch"];

        return null;
    }
}
