using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Fiscal;

/// <summary>
/// Catálogo oficial SAT c_ClaveUnidad, usado para CFDI y como referencia opcional del catálogo propio de Unidad de Medida.
/// </summary>
[Table("cat_claves_unidad_sat")]
public class ClaveUnidadSatCatalogo : BaseEntity<int>
{
    [Required]
    [StringLength(10)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = null!;

    [StringLength(50)]
    public string? Simbolo { get; set; }
}
