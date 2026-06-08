using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Attributes;
using App.Core.Base;
using App.Core.Enums.Billing;
using App.Core.Interfaces;

namespace App.Models.Billing;

[Table("mx_pac_settings")]
public class MexicoPacSettings : BaseEntity<int>, IAuditTracked
{
    // PAC provider authentication
    [Required]
    [StringLength(50)]
    public string ProviderName { get; set; } = null!;

    [StringLength(100)]
    public string? User { get; set; }

    [StringLength(200)]
    [SensitiveData]
    public string? Password { get; set; }

    [StringLength(500)]
    [SensitiveData]
    public string? Token { get; set; }

    [Required]
    [StringLength(200)]
    public string ProductionUrl { get; set; } = null!;

    [StringLength(200)]
    public string? TestUrl { get; set; }

    public bool IsProduction { get; set; }

    // Issuer (emisor) company fiscal data
    [StringLength(20)]
    public string? IssuerRfc { get; set; }

    [StringLength(150)]
    public string? IssuerLegalName { get; set; }

    [StringLength(5)]
    public string? IssuerFiscalRegime { get; set; }

    // Invoice series/folio
    [StringLength(10)]
    public string? InvoiceSerie { get; set; } = "A";

    /// <summary>The folio number to start from (inclusive). Default: 1.</summary>
    public long StartFolio { get; set; } = 1;

    /// <summary>
    /// Total length of the folio string padded with leading zeros.
    /// 0 = no padding (e.g. "42"). Any positive value pads to that length (e.g. 6 → "000042").
    /// </summary>
    public int FolioLength { get; set; } = 0;

    // Global invoice series/folio
    [StringLength(10)]
    public string? GlobalInvoiceSerie { get; set; } = "G";

    /// <summary>The folio number to start from (inclusive) for global invoices. Default: 1.</summary>
    public long GlobalInvoiceStartFolio { get; set; } = 1;

    /// <summary>
    /// Total length of the folio string padded with leading zeros for global invoices.
    /// 0 = no padding. Any positive value pads to that length.
    /// </summary>
    public int GlobalInvoiceFolioLength { get; set; } = 0;

    // CSD (Certificado de Sello Digital) — stored as Base64
    [Column(TypeName = "text")]
    [SensitiveData]
    public string? CsdCertificateBase64 { get; set; }

    [Column(TypeName = "text")]
    [SensitiveData]
    public string? CsdPrivateKeyBase64 { get; set; }

    [StringLength(200)]
    [SensitiveData]
    public string? CsdPassword { get; set; }

    // Issuer address (required for CFDI 4.0 XML)
    [StringLength(10)]
    public string? IssuerPostalCode { get; set; }

    // Auto-invoice behavior
    /// <summary>When enabled, POS shows a "Do you want an invoice?" prompt after each sale to customers with fiscal data.</summary>
    public bool AutoInvoicePromptEnabled { get; set; } = false;

    /// <summary>When true, the invoice prompt allows editing all CFDI fields. When false, only the email is shown.</summary>
    public bool AllowEditFiscalDataInPrompt { get; set; } = true;

    /// <summary>How to resolve CFDI FormaPago when a sale has multiple payment methods.</summary>
    public MultiPaymentFormPolicy MultiPaymentFormPolicy { get; set; } = MultiPaymentFormPolicy.UseHighestAmount;
}
