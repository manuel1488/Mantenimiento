using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateTaxRateDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Required]
    [Range(0, 100)]
    public decimal Rate { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }
}