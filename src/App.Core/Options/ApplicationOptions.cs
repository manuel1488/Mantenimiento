using System.ComponentModel.DataAnnotations;

using App.Core.Options.Validation;

namespace App.Core.Options;

public class ApplicationOptions : IValidatableObject
{
    public const string SectionName = "Application";

    [Required(ErrorMessage = "Application name is required")]
    [StringLength(100, ErrorMessage = "Application name cannot exceed 100 characters")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Version is required")]
    [RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be in format X.Y.Z")]
    public string Version { get; set; } = null!;

    [Required(ErrorMessage = "Default language is required")]
    [ValidDefaultLanguage]
    public string DefaultLanguage { get; set; } = null!;

    [Required(ErrorMessage = "Supported languages are required")]
    [SupportedLanguages]
    public string[] SupportedLanguages { get; set; } = null!;

    [Required(ErrorMessage = "Base URL is required")]
    [Url(ErrorMessage = "Base URL must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;

    // Validaciones adicionales a nivel de objeto
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Verificar que no haya idiomas duplicados
        if (SupportedLanguages?.Distinct().Count() != SupportedLanguages?.Length)
        {
            yield return new ValidationResult(
                "Supported languages cannot contain duplicates",
                new[] { nameof(SupportedLanguages) });
        }
    }
}