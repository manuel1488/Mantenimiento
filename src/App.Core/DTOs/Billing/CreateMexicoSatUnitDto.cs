using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Billing;

public class CreateMexicoSatUnitDto
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string? Symbol { get; set; }
}
