using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Cotizaciones;
using App.Core.Interfaces;
using App.Models.Obras;

namespace App.Models.Cotizaciones;

[Table("cot_cotizaciones")]
public class Cotizacion : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;

    /// <summary>
    /// Versión incremental por Obra (una Obra Rechazada puede regenerar una nueva Cotización, RN §4).
    /// </summary>
    [Required]
    public int Version { get; set; }

    [Required]
    public DateTime FechaGeneracion { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [Required]
    public CotizacionEstado Estado { get; set; } = CotizacionEstado.Pendiente;

    // Aprobación (RN-06): quién, cuándo y por qué medio aprobó el cliente.
    public DateTime? FechaAprobacion { get; set; }

    [StringLength(150)]
    public string? AprobadaPor { get; set; }

    [StringLength(200)]
    public string? MedioAprobacion { get; set; }

    public ICollection<CotizacionLinea> Lineas { get; set; } = new List<CotizacionLinea>();
}
