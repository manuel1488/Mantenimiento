using System.ComponentModel.DataAnnotations;

namespace App.Core.Options;

public class BrandingOptions
{
    public const string SectionName = "Branding";

    [Required(ErrorMessage = "Logo path is required")]
    public string LogoPath { get; set; } = "/images/logo.webp";

    [Required(ErrorMessage = "Favicon path is required")]
    public string FaviconPath { get; set; } = "/favicon.ico";

    [Required(ErrorMessage = "Primary color is required")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Primary color must be a hex value like #1A6868")]
    public string PrimaryColor { get; set; } = "#1A6868";

    [Required(ErrorMessage = "Secondary color is required")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Secondary color must be a hex value like #7B3FA0")]
    public string SecondaryColor { get; set; } = "#7B3FA0";
}
