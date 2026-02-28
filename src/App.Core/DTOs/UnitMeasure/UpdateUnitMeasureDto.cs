using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.UnitMeasure;

public class UpdateUnitMeasureDto
{
    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    public int? MexicoSatUnitId { get; set; }
}