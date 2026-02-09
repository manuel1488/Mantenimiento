using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Billing;

public class UpdateMexicoFiscalCatalogDto
{
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = null!;
}