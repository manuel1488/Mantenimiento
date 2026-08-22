using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_settings")]
public class CompanySettings : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = "UTC";

    [StringLength(200)]
    public string? TimeZoneDisplayName { get; set; }

    /// <summary>
    /// Main brand logo (full color), shown in the NavMenu, Login screen, and generated documents.
    /// </summary>
    public string? LogoBase64 { get; set; }

    /// <summary>
    /// Tasa de IVA por defecto expresada como porcentaje (ej. 16.00 = 16%), usada al generar una
    /// Cotización con "Incluir IVA" activado. Snapshot: la tasa vigente se copia a la Cotización al
    /// crearla, así que cambiar este valor no afecta cotizaciones ya generadas.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal IvaTasaPorDefecto { get; set; } = 16.00m;

    /// <summary>
    /// Domicilio fiscal/comercial de la empresa, texto libre. Mostrado en documentos (ej. pie de la
    /// Cotización) solo cuando el documento correspondiente lo tiene habilitado explícitamente.
    /// </summary>
    [StringLength(300)]
    public string? Direccion { get; set; }
}