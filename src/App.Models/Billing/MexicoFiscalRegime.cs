using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Billing;

[Table("mx_fiscal_regimes")]
public class MexicoFiscalRegime : BaseEntity<int>
{
    [Required]
    [StringLength(5)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = null!;
}