using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

[Table("cat_regimenes_fiscales")]
public class RegimenFiscalCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(5)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Descripcion { get; set; } = null!;
}
