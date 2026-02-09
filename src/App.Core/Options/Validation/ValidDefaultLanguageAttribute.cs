using System.ComponentModel.DataAnnotations;

namespace App.Core.Options.Validation;

public class ValidDefaultLanguageAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var options = validationContext.ObjectInstance as ApplicationOptions;
        if (options is null)
        {
            return new ValidationResult("Invalid options type");
        }

        var defaultLanguage = value as string;
        if (string.IsNullOrEmpty(defaultLanguage))
        {
            return new ValidationResult("Default language must be specified");
        }

        if (options.SupportedLanguages?.Contains(defaultLanguage) != true)
        {
            return new ValidationResult(
                $"Default language '{defaultLanguage}' must be one of the supported languages");
        }

        return ValidationResult.Success;
    }
}