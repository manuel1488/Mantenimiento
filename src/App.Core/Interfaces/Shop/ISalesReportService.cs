using System.Globalization;

using App.Core.DTOs.Reports;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces;

public interface ISalesReportService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(int TotalCount, IList<SaleDto> Items)> GetSalesReportAsync(
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportSalesReportToExcelAsync(
        SalesReportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportSalesReportToPdfAsync(
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default);
}
