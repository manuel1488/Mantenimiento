using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

/// <summary>
/// Singleton row overriding the on-disk default Cotización PDF template
/// (wwwroot/CotizacionTemplates/template.html + styles.css). HTML/CSS are stored separately so each
/// can be edited independently, mirroring <see cref="EmailTemplateSettings"/>. Also holds the
/// optional content sections (payment terms, bank transfer details, company address toggle)
/// shown in the PDF footer regardless of whether the HTML/CSS themselves are overridden.
/// </summary>
[Table("stg_cotizacion_template_settings")]
public class CotizacionTemplateSettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>HTML body content (without &lt;style&gt; block)</summary>
    [Column(TypeName = "longtext")]
    public string HtmlContent { get; set; } = null!;

    /// <summary>CSS styles (stored separately for independent editing)</summary>
    [Column(TypeName = "text")]
    public string CssContent { get; set; } = string.Empty;

    /// <summary>Shown on every Cotización PDF. Null/empty hides the "Términos de Pago" section.</summary>
    [StringLength(2000)]
    public string? PaymentTermsText { get; set; }

    /// <summary>Whether the "Datos para Transferencia" section is shown on the PDF.</summary>
    public bool MostrarDatosBancarios { get; set; }

    [StringLength(150)]
    public string? BancoBeneficiario { get; set; }

    [StringLength(13)]
    public string? BancoRfc { get; set; }

    [StringLength(100)]
    public string? BancoNombre { get; set; }

    [StringLength(50)]
    public string? BancoNumeroCuenta { get; set; }

    [StringLength(18)]
    public string? BancoClabe { get; set; }

    [StringLength(20)]
    public string? BancoSwift { get; set; }

    /// <summary>Whether the company's own address is shown on the PDF.</summary>
    public bool MostrarDireccionEnCotizacion { get; set; }

    [StringLength(300)]
    public string? Direccion { get; set; }

    /// <summary>Whether the "Contacto y Redes Sociales" section is shown on the PDF footer.</summary>
    public bool MostrarContacto { get; set; }

    [StringLength(200)]
    public string? SitioWeb { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(150)]
    public string? CorreoElectronico { get; set; }

    [StringLength(30)]
    public string? WhatsApp { get; set; }

    [StringLength(150)]
    public string? Facebook { get; set; }

    [StringLength(150)]
    public string? Instagram { get; set; }
}
