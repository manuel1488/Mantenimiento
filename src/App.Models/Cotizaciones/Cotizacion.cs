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
}
