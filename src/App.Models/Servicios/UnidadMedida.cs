using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;
using App.Models.Fiscal;

namespace App.Models.Servicios;

/// <summary>
/// Catálogo propio de unidades de medida para Servicios, opcionalmente vinculado a una clave del catálogo SAT.
/// </summary>
[Table("srv_unidades_medida")]
public class UnidadMedida : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(20)]
    public string Codigo { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public int? ClaveUnidadSatId { get; set; }

    [ForeignKey(nameof(ClaveUnidadSatId))]
    public virtual ClaveUnidadSatCatalogo? ClaveUnidadSat { get; set; }
}
