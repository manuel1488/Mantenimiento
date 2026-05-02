using System.Globalization;

using App.Core.DTOs.Billing;

namespace App.Core.Interfaces.Billing;

public interface IInvoiceReportService
{
    Task<byte[]> ExportIndividualInvoicesAsync(InvoiceReportRequestDto request, CultureInfo culture, CancellationToken ct = default);
    Task<byte[]> ExportGlobalInvoicesAsync(InvoiceReportRequestDto request, CultureInfo culture, CancellationToken ct = default);
    Task<byte[]> ExportConciliationAsync(InvoiceReportRequestDto request, CultureInfo culture, CancellationToken ct = default);
    Task<byte[]> ExportVatReportAsync(InvoiceReportRequestDto request, CultureInfo culture, CancellationToken ct = default);
    Task<byte[]> ExportSalesBookAsync(InvoiceReportRequestDto request, CultureInfo culture, CancellationToken ct = default);
}
