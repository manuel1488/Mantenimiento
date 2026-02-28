using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Billing;

[Table("mx_cfdi_uses")]
public class MexicoCfdiUse : BaseEntity<int>
{
    [Required]
    [StringLength(5)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(250)]
    public string Description { get; set; } = null!;

    /// <summary>Comma-separated fiscal regime codes that can use this CFDI use (null = applies to all).</summary>
    [StringLength(500)]
    public string? FiscalRegimeCodes { get; set; }
}