using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace App.Core.Options.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class SupportedLanguagesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return new ValidationResult("Supported languages cannot be null");
        }

        if (value is not string[] languages || languages.Length == 0)
        {
            return new ValidationResult("At least one supported language must be specified");
        }

        var invalidLanguages = new List<string>();

        foreach (var language in languages)
        {
            try
            {
                _ = new CultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                invalidLanguages.Add(language);
            }
        }

        if (invalidLanguages.Any())
        {
            return new ValidationResult(
                $"The following language codes are invalid: {string.Join(", ", invalidLanguages)}");
        }

        return ValidationResult.Success;
    }
}