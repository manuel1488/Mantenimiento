using System.ComponentModel.DataAnnotations;
using App.Core.Enums.Settings;

namespace App.Core.DTOs.Settings;

public class UpdateRoundingSettingsDto
{
    [Required]
    public bool IsEnabled { get; set; }

    [Required]
    public RoundingMethod Method { get; set; }

    [Required]
    [Range(0, 2)]
    public int DecimalPlaces { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinimumThreshold { get; set; }
}
