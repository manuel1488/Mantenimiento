using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Enums.Shop;

namespace App.Core.Interfaces.Shop;

public interface IQuotationService
{
    Task<(int TotalCount, IList<QuotationDto> Items)> GetQuotationsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        long? customerId = null,
        string? status = null);

    Task<QuotationDto?> GetByIdAsync(long id);

    Task<Result<QuotationDto>> CreateAsync(CreateQuotationDto dto);

    Task<Result<QuotationDto>> UpdateAsync(long id, UpdateQuotationDto dto);

    Task<Result> DeleteAsync(long id);

    Task<Result> UpdateStatusAsync(long id, QuotationStatus status, string? reason = null);

    /// <summary>
    /// Checks for discrepancies (catalog price changes, active rounding, tax rate
    /// changes) between the quotation and current settings, so the operator can be
    /// warned before converting it to a sale. Purely informational — the conversion
    /// always honors the locked quoted prices and total.
    /// </summary>
    Task<Result<QuotationConversionCheckDto>> CheckConversionDiscrepanciesAsync(long id);

    Task<Result> SendByEmailAsync(long id, string? emailOverride = null);

    Task<byte[]> GeneratePdfAsync(long id);

    Task<Result<byte[]>> GetPreInvoicePdfAsync(long id);
}
