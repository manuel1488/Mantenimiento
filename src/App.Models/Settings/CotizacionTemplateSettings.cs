using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

/// <summary>
/// Singleton row overriding the on-disk default Cotización PDF template
/// (wwwroot/CotizacionTemplates/default.html). HTML/CSS are stored separately so each
/// can be edited independently, mirroring <see cref="EmailTemplateSettings"/>.
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
}
