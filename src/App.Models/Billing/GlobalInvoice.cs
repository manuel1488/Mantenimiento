using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Billing;

namespace App.Models.Billing;

[Table("mx_global_invoices")]
public class GlobalInvoice : BaseEntity<long>
{
    // CFDI identification
    [StringLength(10)]
    public string? Serie { get; set; }

    public long Folio { get; set; }

    [StringLength(36)]
    public string? Uuid { get; set; }

    // Period
    public GlobalInvoicePeriodicity Periodicity { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Zero-padded month for InformacionGlobal/@Meses, e.g. "03".</summary>
    [Required]
    [StringLength(2)]
    public string PeriodMonth { get; set; } = null!;

    public int PeriodYear { get; set; }

    // Payment
    /// <summary>SAT PaymentForm code, e.g. "01"=Cash, "99"=Por definir.</summary>
    [Required]
    [StringLength(5)]
    public string PaymentForm { get; set; } = null!;

    // Summary
    public int SaleCount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Total { get; set; }

    // Stamping
    public GlobalInvoiceStatus Status { get; set; } = GlobalInvoiceStatus.Draft;

    [Column(TypeName = "mediumtext")]
    public string? XmlContent { get; set; }

    public DateTime? StampDate { get; set; }

    [StringLength(1000)]
    public string? StampError { get; set; }

    // Cancellation
    public DateTime? CancellationDate { get; set; }

    [StringLength(2)]
    public string? CancellationReason { get; set; }

    [StringLength(36)]
    public string? ReplacementUuid { get; set; }

    [StringLength(20)]
    public string? CancellationStatus { get; set; }

    [Column(TypeName = "mediumtext")]
    public string? CancellationAcuse { get; set; }

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

    public virtual ICollection<GlobalInvoiceSale> GlobalInvoiceSales { get; set; } = new List<GlobalInvoiceSale>();
}
