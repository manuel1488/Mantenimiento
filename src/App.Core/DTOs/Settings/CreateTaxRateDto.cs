using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class CreateTaxRateDto
{
    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [Range(0, 100)]
    public decimal Rate { get; set; }

    [Required]
    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsDefault { get; set; }

    [StringLength(2)]
    public string? ProvinceCode { get; set; }

    [StringLength(20)]
    public string? Type { get; set; }

    public bool IsActive { get; set; }
}