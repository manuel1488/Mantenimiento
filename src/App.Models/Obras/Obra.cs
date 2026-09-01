using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Models.Clientes;
using App.Models.Cotizaciones;
using App.Models.Facturas;

namespace App.Models.Obras;

[Table("obr_obras")]
public class Obra : BaseEntity<int>, IAuditTracked
{
    /// <summary>
    /// Año y consecutivo del folio (ej. Año=2026, Numero=42 → "OBR-2026-0042"), asignados al crear la
    /// Obra; el consecutivo se reinicia cada año. El prefijo/padding se aplican en tiempo de
    /// visualización desde ObraFolioSettings (mismo esquema que Cotizacion.FolioAnio/FolioNumero).
    /// Nulos en Obras creadas antes de que existiera este folio.
    /// </summary>
    public int? FolioAnio { get; set; }
    public int? FolioNumero { get; set; }

    [Required]
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    /// <summary>
    /// Cotización aprobada de la que se generó esta Obra, si fue creada por conversión en vez de
    /// capturada manualmente. Relación 1:1 opcional — una Cotización solo puede convertirse una vez.
    /// </summary>
    public int? CotizacionOrigenId { get; set; }
    public Cotizacion? CotizacionOrigen { get; set; }

    /// <summary>
    /// Dirección de la obra, texto libre capturado por Obra, sin catálogo de sitios.
    /// </summary>
    [Required]
    [StringLength(300)]
    public string Direccion { get; set; } = null!;

    public bool Urgente { get; set; }

    [Required]
    public ObraEstado Estado { get; set; } = ObraEstado.Solicitada;

    [Required]
    public DateTime FechaSolicitud { get; set; }

    public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();

    /// <summary>Fotos generales de la Obra, no asociadas a ninguna Actividad en particular.</summary>
    public ICollection<ObraFotoGeneral> FotosGenerales { get; set; } = new List<ObraFotoGeneral>();

    public Factura? Factura { get; set; }
}
