using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Servicios;

[Table("srv_servicios")]
public class Servicio : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    public int UnidadMedidaId { get; set; }

    [ForeignKey(nameof(UnidadMedidaId))]
    public virtual UnidadMedida UnidadMedida { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Rendimiento estimado en días por unidad, usado para calcular el tiempo estimado de una Actividad.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal RendimientoDiasPorUnidad { get; set; }
}
