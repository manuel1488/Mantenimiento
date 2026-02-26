using App.Core.Common;
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

    /// <summary>Requests cancellation via SAT/PAC.</summary>
    Task<Result> CancelAsync(long invoiceId, string reason);

    /// <summary>Returns the next folio number for the configured serie.</summary>
    Task<long> GetNextFolioAsync();

    /// <summary>Validates that a sale can be invoiced (completed, not already invoiced, etc.).</summary>
    Task<Result> ValidateSaleForInvoicingAsync(long saleId);
}
