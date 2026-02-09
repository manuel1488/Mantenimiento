using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Billing;

public abstract class CreateMexicoFiscalCatalogDto
{
    [Required]
    [StringLength(5)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = null!;

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}