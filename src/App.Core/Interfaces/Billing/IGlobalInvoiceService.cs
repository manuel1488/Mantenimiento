using App.Core.Common;
using App.Core.DTOs.Billing;

namespace App.Core.Interfaces.Billing;

public interface IGlobalInvoiceService
{
    /// <summary>
    /// Returns a preview of totals for sales eligible for a global invoice in the given date range
    /// (sales without an active CFDI and not already included in an active global invoice).
    /// </summary>
    Task<Result<GlobalInvoicePreviewDto>> PreviewAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Generates the CFDI XML, stamps it via PAC, persists the GlobalInvoice record
    /// and links the included sales.
    /// </summary>
    Task<Result<GlobalInvoiceDto>> CreateAndStampAsync(CreateGlobalInvoiceDto dto);

    Task<Result<List<GlobalInvoiceListDto>>> GetAllAsync();

    Task<Result<GlobalInvoiceDto>> GetByIdAsync(long id);

    /// <summary>Returns the signed+stamped XML content.</summary>
    Task<Result<string>> GetXmlAsync(long id);

    /// <summary>Generates (or regenerates) the human-readable PDF for a stamped invoice.</summary>
    Task<Result<byte[]>> GetPdfAsync(long id);

    /// <summary>
    /// Generates a preview PDF from unsaved form data. No DB write, no stamping.
    /// The PDF includes a "VISTA PREVIA" banner and omits digital seals.
    /// </summary>
    Task<Result<byte[]>> GetPdfPreviewAsync(CreateGlobalInvoiceDto dto, GlobalInvoicePreviewDto preview);

    /// <summary>Requests cancellation of the CFDI via PAC.</summary>
    Task<Result> CancelAsync(long id, string reason, string? replacementUuid = null, string? notes = null);

    /// <summary>Returns the cancellation acuse XML bytes.</summary>
    Task<Result<byte[]>> GetCancellationAcuseAsync(long id);

    /// <summary>
    /// Returns a map of SaleId → GlobalInvoiceId for sales included in Stamped (active) global invoices.
    /// Used to show invoice status indicators in the sales history.
    /// </summary>
    Task<Result<Dictionary<long, long>>> GetActiveSaleToInvoiceMapAsync();
}
