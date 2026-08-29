using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;
using App.Models.Servicios;

namespace App.Models.Cotizaciones;

/// <summary>
/// Línea independiente de una Cotización: Servicio + Cantidad capturados directamente,
/// sin relación a ninguna Actividad. Snapshot de nombre/unidad/precio al momento de
/// cotizar, para preservar el precio cotizado aunque el catálogo de Servicio cambie después.
/// </summary>
[Table("cot_cotizacion_lineas")]
public class CotizacionLinea : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    [Required]
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string ServicioNombre { get; set; } = null!;

    /// <summary>
    /// Descripción de esta línea, copiada del catálogo de Servicio al agregarla pero editable
    /// libremente por línea sin afectar el catálogo — igual que ServicioNombre/UnidadMedida/PrecioUnitario,
    /// es un snapshot que preserva lo cotizado aunque el Servicio cambie después.
    /// </summary>
    [StringLength(3000)]
    public string? Descripcion { get; set; }

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

    public ICollection<CotizacionFoto> Fotos { get; set; } = new List<CotizacionFoto>();
}
