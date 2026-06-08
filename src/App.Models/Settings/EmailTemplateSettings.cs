using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_email_template_settings")]
public class EmailTemplateSettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>Template name, e.g. "invoice-cfdi"</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>HTML body content (without &lt;style&gt; block)</summary>
    [Column(TypeName = "longtext")]
    public string HtmlContent { get; set; } = null!;

    /// <summary>CSS styles (stored separately for independent editing)</summary>
    [Column(TypeName = "text")]
    public string CssContent { get; set; } = string.Empty;
}
