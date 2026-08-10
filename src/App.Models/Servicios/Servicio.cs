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

    /// <summary>
    /// Unidad de medida fija del catálogo (m², m³, pieza, etc.), texto libre sin catálogo propio.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UnidadMedida { get; set; } = null!;

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
