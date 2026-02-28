using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Billing;

public class CreateMexicoCfdiUseDto : CreateMexicoFiscalCatalogDto
{
    [StringLength(500)]
    public string? FiscalRegimeCodes { get; set; }
}