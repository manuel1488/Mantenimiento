using App.Core.Common;
using App.Core.DTOs.Billing;
using App.Core.DTOs.Billing.Mexico;

namespace App.Core.Interfaces.Billing;

public interface IMexicoInvoiceService
{
    /// <summary>Creates and stamps a CFDI invoice for a completed sale.</summary>
    Task<Result<MexicoInvoiceDto>> CreateAndStampAsync(CreateMexicoInvoiceDto dto);

    /// <summary>Returns the invoice for a sale, or null if not invoiced.</summary>
    Task<MexicoInvoiceDto?> GetBySaleIdAsync(long saleId);

    /// <summary>Returns an invoice by its internal ID.</summary>
    Task<MexicoInvoiceDto?> GetByIdAsync(long id);

    /// <summary>Paginated invoice history with optional filters.</summary>
    Task<(int TotalCount, IList<MexicoInvoiceSummaryDto> Items)> GetHistoryAsync(
        int page = 1,
        int pageSize = 20,
        string? searchString = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null);

    /// <summary>Returns the stamped CFDI XML bytes.</summary>
    Task<Result<byte[]>> GetXmlAsync(long invoiceId);

    /// <summary>Returns the invoice PDF bytes.</summary>
    Task<Result<byte[]>> GetPdfAsync(long invoiceId);

    /// <summary>Sends the invoice by email to the specified address.</summary>
    Task<Result> SendByEmailAsync(long invoiceId, string email);

    /// <summary>
    /// Sends a cancellation request to SAT via PAC and stores the acuse.
    /// </summary>
    /// <param name="invoiceId">Invoice to cancel.</param>
    /// <param name="cancellationReason">SAT reason code: 01 (con relación), 02 (sin relación), 03 (no se llevó a cabo), 04 (nominativa global).</param>
    /// <param name="replacementUuid">Replacement invoice UUID — required when reason is "01".</param>
    Task<Result> CancelAsync(long invoiceId, string cancellationReason, string? replacementUuid = null);

    /// <summary>Returns the SAT cancellation acknowledgment XML bytes for download.</summary>
    Task<Result<byte[]>> GetCancellationAcuseAsync(long invoiceId);

    /// <summary>
    /// Queries SAT via PAC and updates status on a CancellationPending invoice.
    /// Updates to Cancelled/Accepted, Stamped/Rejected, or leaves as Pending.
    /// </summary>
    Task<Result> RefreshCancellationStatusAsync(long invoiceId);

    /// <summary>Returns the next folio number for the configured serie.</summary>
    Task<long> GetNextFolioAsync();

    /// <summary>Validates that a sale can be invoiced (completed, not already invoiced, etc.).</summary>
    Task<Result> ValidateSaleForInvoicingAsync(long saleId);

    /// <summary>Returns basic sale info for the post-sale invoicing flow (lookup step).</summary>
    Task<Result<SaleForInvoicingDto>> GetSaleInfoForInvoicingAsync(long saleId);
}
