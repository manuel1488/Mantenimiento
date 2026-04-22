using App.Core.Enums.Billing;

namespace App.Core.DTOs.Billing;

public class GlobalInvoiceListDto
{
    public long Id { get; set; }
    public string? Serie { get; set; }
    public long Folio { get; set; }
    public string? Uuid { get; set; }
    public GlobalInvoicePeriodicity Periodicity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PaymentForm { get; set; } = string.Empty;
    public int SaleCount { get; set; }
    public decimal Total { get; set; }
    public GlobalInvoiceStatus Status { get; set; }
    public DateTime? StampDate { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationNotes { get; set; }
    public string? CancellationStatus { get; set; }
    public bool HasCancellationAcuse { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class GlobalInvoiceDto : GlobalInvoiceListDto
{
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string PeriodMonth { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public string IssuerRfc { get; set; } = string.Empty;
    public string IssuerLegalName { get; set; } = string.Empty;
    public string IssuerFiscalRegime { get; set; } = string.Empty;
    public string IssuerPostalCode { get; set; } = string.Empty;
    public string? StampError { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationStatus { get; set; }
}

public class CreateGlobalInvoiceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public GlobalInvoicePeriodicity Periodicity { get; set; } = GlobalInvoicePeriodicity.Monthly;
    /// <summary>SAT PaymentForm code e.g. "01", "28", "99".</summary>
    public string PaymentForm { get; set; } = "99";
    /// <summary>
    /// When non-empty, only the specified eligible sale IDs will be included.
    /// When null or empty, all eligible sales in the date range are included.
    /// </summary>
    public List<long>? SelectedSaleIds { get; set; }
}

public class GlobalInvoicePdfDto
{
    public string FolioDisplay { get; set; } = string.Empty;
    public string? Uuid { get; set; }
    /// <summary>Pre-formatted stamp date in company timezone (e.g. "01/04/2026 10:35").</summary>
    public string? StampDateLocal { get; set; }
    /// <summary>Pre-formatted period start date in company timezone.</summary>
    public string StartDateLocal { get; set; } = string.Empty;
    /// <summary>Pre-formatted period end date in company timezone.</summary>
    public string EndDateLocal { get; set; } = string.Empty;
    public string PeriodMonth { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public string PaymentForm { get; set; } = string.Empty;
    public string PaymentFormDescription { get; set; } = string.Empty;
    public int SaleCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public GlobalInvoiceStatus Status { get; set; }
    // Issuer
    public string IssuerRfc { get; set; } = string.Empty;
    public string IssuerLegalName { get; set; } = string.Empty;
    public string IssuerFiscalRegime { get; set; } = string.Empty;
    public string IssuerPostalCode { get; set; } = string.Empty;
    // Digital seals
    public string? NoCertificadoCfdi { get; set; }
    public string? SelloCfdi { get; set; }
    public string? SelloSat { get; set; }
    // Presentation
    public string CompanyName { get; set; } = string.Empty;
    public string LogoBase64 { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "$";
    /// <summary>When true the PDF view renders a "VISTA PREVIA" watermark and hides digital seals.</summary>
    public bool IsPreview { get; set; }
}

public class GlobalInvoicePreviewDto
{
    public int SaleCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    /// <summary>Sales already with an active individual CFDI — excluded from global invoice.</summary>
    public int AlreadyInvoicedCount { get; set; }
    public List<GlobalInvoicePreviewSaleDto> Sales { get; set; } = [];
}

public class GlobalInvoicePreviewSaleDto
{
    public long SaleId { get; set; }
    /// <summary>Sale date pre-formatted in company timezone.</summary>
    public string SaleDateLocal { get; set; } = string.Empty;
    public string PaymentMethods { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
}
