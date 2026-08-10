using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;
using App.Models.Obras;

namespace App.Models.Cotizaciones;

/// <summary>
/// Snapshot de una Actividad al momento de generar la Cotización: preserva el precio
/// cotizado aunque el catálogo de Servicio cambie después.
/// </summary>
[Table("cot_cotizacion_lineas")]
public class CotizacionLinea : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    [Required]
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string ServicioNombre { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string UnidadMedida { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal Cantidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }
}
