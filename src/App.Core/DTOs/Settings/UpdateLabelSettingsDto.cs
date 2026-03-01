using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateLabelSettingsDto
{
    [Required]
    [Range(20, 200, ErrorMessage = "Width must be between 20 and 200 mm")]
    public int WidthMm { get; set; } = 62;

    [Required]
    [Range(10, 300, ErrorMessage = "Height must be between 10 and 300 mm")]
    public int HeightMm { get; set; } = 28;
}
