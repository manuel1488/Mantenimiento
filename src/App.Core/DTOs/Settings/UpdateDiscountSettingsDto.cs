using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateDiscountSettingsDto
{
    [Required]
    public bool RequireAuthorizationForPublicDiscount { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal MaximumPublicDiscount { get; set; }
}
