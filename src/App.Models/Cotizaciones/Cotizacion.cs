using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Cotizaciones;
using App.Core.Interfaces;
using App.Models.Clientes;

namespace App.Models.Cotizaciones;

[Table("cot_cotizaciones")]
public class Cotizacion : BaseEntity<int>, IAuditTracked
{
    [Required]
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    [Required]
    public DateTime FechaGeneracion { get; set; }

    /// <summary>
    /// Año y consecutivo del folio (ej. Año=2026, Numero=42 → "COT-2026-0042"), asignados al crear la
    /// Cotización; el consecutivo se reinicia cada año. El prefijo/padding se aplican en tiempo de
    /// visualización desde CotizacionTemplateSettings, así que cambiarlos no altera folios ya
    /// asignados. Nulos en Cotizaciones creadas antes de que existiera este folio.
    /// </summary>
    public int? FolioAnio { get; set; }
    public int? FolioNumero { get; set; }

    /// <summary>Suma de los subtotales de las líneas, sin IVA.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    /// <summary>Si esta Cotización incluye IVA sobre el Subtotal.</summary>
    public bool IncluirIva { get; set; }

    /// <summary>
    /// Tasa de IVA aplicada, como porcentaje (ej. 16.00 = 16%). Snapshot de
    /// <see cref="Models.Settings.CompanySettings.IvaTasaPorDefecto"/> al momento de crear/editar la
    /// Cotización — cambiar la tasa por defecto después no debe alterar cotizaciones ya generadas.
    /// Cero cuando <see cref="IncluirIva"/> es falso.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal IvaTasa { get; set; }

    /// <summary>Monto de IVA = Subtotal * IvaTasa / 100. Cero cuando <see cref="IncluirIva"/> es falso.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal IvaMonto { get; set; }

    /// <summary>Total a cobrar = Subtotal + IvaMonto.</summary>
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
    public ICollection<CotizacionFoto> Fotos { get; set; } = new List<CotizacionFoto>();

    /// <summary>
    /// Huella SHA-256 (ver CotizacionIntegrityHasher) sobre el folio, cliente, totales y líneas —
    /// recalculada en cada creación/edición. No es una firma digital (no usa llave privada); solo
    /// permite detectar si los datos guardados fueron alterados directamente en la base de datos
    /// sin pasar por la aplicación.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string IntegridadHash { get; set; } = string.Empty;
}
