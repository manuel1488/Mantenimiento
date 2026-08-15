using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

[Table("cat_usos_cfdi")]
public class UsoCfdiCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(5)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(250)]
    public string Descripcion { get; set; } = null!;

    /// <summary>Códigos de régimen fiscal (separados por coma) para los que aplica este uso de CFDI (null = aplica a todos).</summary>
    [StringLength(500)]
    public string? CodigosRegimenFiscal { get; set; }
}
