using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_quotation_settings")]
public class QuotationSettings : BaseEntity<long>
{
    // ── Payment terms ─────────────────────────────────────────────────────────
    /// <summary>Shown in the quotation PDF footer. Null = section hidden.</summary>
    [StringLength(2000)]
    public string? PaymentTermsText { get; set; }

    // ── Bank / wire-transfer details ──────────────────────────────────────────
    public bool ShowBankDetails { get; set; } = false;

    [StringLength(150)]
    public string? BankBeneficiary { get; set; }

    [StringLength(30)]
    public string? BankRfc { get; set; }

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    /// <summary>CLABE interbancaria (18 digits, Mexico).</summary>
    [StringLength(20)]
    public string? BankClabeNumber { get; set; }

    [StringLength(20)]
    public string? BankSwift { get; set; }

    // ── Contact / social media for footer ────────────────────────────────────
    public bool ShowContactInfo { get; set; } = false;

    [StringLength(200)]
    public string? ContactWebsite { get; set; }

    [StringLength(200)]
    public string? ContactFacebook { get; set; }

    [StringLength(200)]
    public string? ContactInstagram { get; set; }

    [StringLength(20)]
    public string? ContactWhatsapp { get; set; }

    [StringLength(20)]
    public string? ContactPhone { get; set; }

    [StringLength(100)]
    public string? ContactEmail { get; set; }

    // ── Template customisation ────────────────────────────────────────────────
    /// <summary>Extra CSS injected into the quotation template. Overrides default colours/fonts.</summary>
    public string? CustomCss { get; set; }

    /// <summary>Full HTML body to replace the default Razor template (mirrors EmailTemplateSettings.HtmlContent).</summary>
    public string? HtmlBody { get; set; }
}
