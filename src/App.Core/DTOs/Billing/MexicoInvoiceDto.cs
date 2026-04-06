using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Billing.Mexico;

public class MexicoInvoiceSummaryDto : AuditableDto
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public string? Serie { get; set; }
    public long Folio { get; set; }
    public string FolioDisplay => string.IsNullOrEmpty(Serie) ? $"{Folio}" : $"{Serie}{Folio}";
    public string? Uuid { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsStamped { get; set; }
    public DateTime? StampDate { get; set; }
    /// <summary>UTC date/time the user requested for the CFDI Fecha. Null = invoice was issued at stamp time.</summary>
    public DateTime? RequestedInvoiceDate { get; set; }
    public string CustomerRfc { get; set; } = string.Empty;
    public string CustomerLegalName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CfdiUse { get; set; } = string.Empty;
    public string? CancellationStatus { get; set; }
    public DateTime? CancellationDate { get; set; }
    public bool HasCancellationAcuse { get; set; }
    public bool HasXml { get; set; }
    public bool HasPdf { get; set; }
    public string? StampError { get; set; }
    public string? CustomerEmail { get; set; }
}

public class MexicoInvoiceDto : MexicoInvoiceSummaryDto
{
    public string PaymentForm { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string CustomerPostalCode { get; set; } = string.Empty;
    public string CustomerFiscalRegime { get; set; } = string.Empty;
    public string IssuerRfc { get; set; } = string.Empty;
    public string IssuerLegalName { get; set; } = string.Empty;
    public string IssuerFiscalRegime { get; set; } = string.Empty;
    public string IssuerPostalCode { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public string Currency { get; set; } = "MXN";
    public string? NoCertificadoSat { get; set; }
    public string? NoCertificadoCfdi { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? StampError { get; set; }
    public bool HasXml { get; set; }
    public bool HasPdf { get; set; }
}
