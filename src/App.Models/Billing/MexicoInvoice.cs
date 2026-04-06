using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Models.Shop;

namespace App.Models.Billing;

[Table("mx_invoices")]
public class MexicoInvoice : BaseEntity<long>
{
    public long SaleId { get; set; }

    // CFDI identification
    [StringLength(10)]
    public string? Serie { get; set; }

    public long Folio { get; set; }

    [StringLength(36)]
    public string? Uuid { get; set; }

    // CFDI fiscal data
    [Required]
    [StringLength(5)]
    public string CfdiUse { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string PaymentForm { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string PaymentMethod { get; set; } = null!;

    // Snapshot of customer fiscal data at invoice time
    [Required]
    [StringLength(20)]
    public string CustomerRfc { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string CustomerLegalName { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string CustomerPostalCode { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string CustomerFiscalRegime { get; set; } = null!;

    // Issuer snapshot
    [Required]
    [StringLength(20)]
    public string IssuerRfc { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string IssuerLegalName { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string IssuerFiscalRegime { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string IssuerPostalCode { get; set; } = null!;

    // Amounts (snapshot from sale)
    [Column(TypeName = "decimal(18,4)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Total { get; set; }

    [StringLength(5)]
    public string Currency { get; set; } = "MXN";

    [Column(TypeName = "decimal(18,6)")]
    public decimal ExchangeRate { get; set; } = 1;

    /// <summary>UTC date/time the user requested for the CFDI Fecha. Null means the invoice was issued at stamp time.</summary>
    public DateTime? RequestedInvoiceDate { get; set; }

    // Stamping status
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public bool IsStamped { get; set; }

    public DateTime? StampDate { get; set; }

    // Cancellation
    public DateTime? CancellationDate { get; set; }

    /// <summary>SAT reason code: 01, 02, 03, 04.</summary>
    [StringLength(2)]
    public string? CancellationReason { get; set; }

    /// <summary>Replacement invoice UUID (required for reason 01).</summary>
    [StringLength(36)]
    public string? ReplacementUuid { get; set; }

    /// <summary>Pending / Accepted / Rejected / null.</summary>
    [StringLength(20)]
    public string? CancellationStatus { get; set; }

    /// <summary>SAT-signed cancellation acknowledgment XML from PAC.</summary>
    [Column(TypeName = "mediumtext")]
    public string? CancellationAcuse { get; set; }

    /// <summary>SAT invoice status at cancellation time: "Cancelado", "Vigente".</summary>
    [StringLength(50)]
    public string? CancellationStatusSat { get; set; }

    /// <summary>"Cancelable sin aceptación", "Cancelable con aceptación", "No cancelable".</summary>
    [StringLength(100)]
    public string? CancellationIsCancelable { get; set; }

    /// <summary>UUID status code returned by PAC: 201, 202, 204, etc.</summary>
    [StringLength(10)]
    public string? CancellationUuidStatusCode { get; set; }

    // Digital seals from PAC
    [StringLength(20)]
    public string? NoCertificadoSat { get; set; }

    [StringLength(20)]
    public string? NoCertificadoCfdi { get; set; }

    [Column(TypeName = "text")]
    public string? SelloSat { get; set; }

    [Column(TypeName = "text")]
    public string? SelloCfdi { get; set; }

    [Column(TypeName = "text")]
    public string? CadenaOriginalSat { get; set; }

    // Error from PAC (if stamping failed)
    [StringLength(1000)]
    public string? StampError { get; set; }

    [ForeignKey(nameof(SaleId))]
    public virtual Sale Sale { get; set; } = null!;

    public virtual ICollection<MexicoInvoiceFile> Files { get; set; } = new List<MexicoInvoiceFile>();
}
