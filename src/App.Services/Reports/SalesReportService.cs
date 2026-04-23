using System.Drawing;
using System.Globalization;

using AutoMapper;

using App.Core.DTOs.Reports;
using App.Core.DTOs.Shop;
using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace App.Services.Reports;

public class SalesReportService : ISalesReportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<SalesReportService> _logger;
    private readonly IStringLocalizer<SalesReportService> _localizer;
    private readonly IPdfService _pdfService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IDateTime _dateTime;
    private readonly ExportOptions _exportOptions;

    public SalesReportService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<SalesReportService> logger,
        IStringLocalizer<SalesReportService> localizer,
        IPdfService pdfService,
        ICompanySettingsService companySettingsService,
        IDateTime dateTime,
        IOptions<ExportOptions> exportOptions)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _pdfService = pdfService;
        _companySettingsService = companySettingsService;
        _dateTime = dateTime;
        _exportOptions = exportOptions.Value;
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Base query con filtros
            var query = await GetBaseQueryAsync(context, request, cancellationToken);

            // Get basic summary data
            var totalCount = await query.CountAsync(cancellationToken);
            var totalRevenue = await query.SumAsync(s => s.Total, cancellationToken);
            var totalTax = await query.SumAsync(s => s.TaxAmount, cancellationToken);
            var totalDiscount = await query.SumAsync(s => s.DiscountAmount, cancellationToken);

            // Sales grouped by type
            var salesByType = await query
                .GroupBy(s => s.SaleType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    k => k.Type.ToString(),
                    v => v.Count,
                    cancellationToken);

            // Sales grouped by status
            var salesByStatus = await query
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    k => k.Status.ToString(),
                    v => v.Count,
                    cancellationToken);

            // Sales grouped by payment method (via SalePayments join)
            var salesByPaymentMethod = await query
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod.Name)
                .Select(g => new { Method = g.Key, Count = g.Select(p => p.SaleId).Distinct().Count() })
                .ToDictionaryAsync(
                    k => k.Method,
                    v => v.Count,
                    cancellationToken);

            // Sales amount grouped by payment method
            var salesByPaymentMethodAmount = await query
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod.Name)
                .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(
                    k => k.Method,
                    v => v.Total,
                    cancellationToken);

            // Top 5 sales by value
            var topSales = await query
                .OrderByDescending(s => s.Total)
                .Take(5)
                .Include(s => s.Customer)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .Select(s => _mapper.Map<SaleDto>(s))
                .ToListAsync(cancellationToken);

            // Sales grouped by date
            var salesByDate = await query
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new SaleGroupedByDateDto
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(s => s.Total)
                })
                .OrderBy(g => g.Date)
                .ToListAsync(cancellationToken);

            // Obtener fechas en la zona horaria correcta si están especificadas
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;
            var startDate = request.StartDate.HasValue ? request.StartDate.Value : _dateTime.Now.AddMonths(-1);
            var endDate = request.EndDate.HasValue ? request.EndDate.Value : _dateTime.Now;

            return new SalesSummaryDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalSales = totalCount,
                TotalRevenue = totalRevenue,
                TotalTax = totalTax,
                TotalDiscount = totalDiscount,
                SalesByType = salesByType,
                SalesByStatus = salesByStatus,
                SalesByPaymentMethod = salesByPaymentMethod,
                SalesByPaymentMethodAmount = salesByPaymentMethodAmount,
                TopSales = topSales,
                SalesByDate = salesByDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales summary");
            throw;
        }
    }

    public async Task<(int TotalCount, IList<SaleDto> Items)> GetSalesReportAsync(
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Base query con filtros
            var query = await GetBaseQueryAsync(context, request, cancellationToken);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Get paginated results
            var items = await query
                .OrderBy(s => s.SaleDate)
                .Include(s => s.Customer)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .Skip(0)
                .Take(request.PageSize)
                .Select(s => _mapper.Map<SaleDto>(s))
                .ToListAsync(cancellationToken);

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales report");
            throw;
        }
    }

    public async Task<byte[]> ExportSalesReportToExcelAsync(
        SalesReportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar límite de registros para exportación
            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                throw new InvalidOperationException(
                    $"Export request exceeds maximum allowed records ({_exportOptions.MaxExportRecords})");
            }

            // Get report data
            var (_, items) = await GetSalesReportAsync(request, cancellationToken);
            var summary = await GetSalesSummaryAsync(request, cancellationToken);

            // Obtener la zona horaria de la empresa
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            // Create Excel package
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            // Create Summary worksheet
            var summarySheet = package.Workbook.Worksheets.Add(_localizer["Summary"]);

            // Add headers
            summarySheet.Cells[1, 1].Value = _localizer["Sales Report"];
            summarySheet.Cells[2, 1].Value = _localizer["Period"];

            // Formatear fechas usando el servicio DateTime
            var startDateStr = _dateTime.FormatToTimezone(summary.StartDate, timeZone);
            var endDateStr = _dateTime.FormatToTimezone(summary.EndDate, timeZone);
            summarySheet.Cells[2, 2].Value = $"{startDateStr} - {endDateStr}";

            summarySheet.Cells[4, 1].Value = _localizer["Total Sales"];
            summarySheet.Cells[4, 2].Value = summary.TotalSales;

            summarySheet.Cells[5, 1].Value = _localizer["Total Revenue"];
            summarySheet.Cells[5, 2].Value = summary.TotalRevenue;
            summarySheet.Cells[5, 2].Style.Numberformat.Format = GetNumberFormat(culture);

            summarySheet.Cells[6, 1].Value = _localizer["Total Tax"];
            summarySheet.Cells[6, 2].Value = summary.TotalTax;
            summarySheet.Cells[6, 2].Style.Numberformat.Format = GetNumberFormat(culture);

            summarySheet.Cells[7, 1].Value = _localizer["Total Discount"];
            summarySheet.Cells[7, 2].Value = summary.TotalDiscount;
            summarySheet.Cells[7, 2].Style.Numberformat.Format = GetNumberFormat(culture);

            // Sales by Type
            summarySheet.Cells[9, 1].Value = _localizer["Sales by Type"];
            var typeRow = 10;
            foreach (var type in summary.SalesByType)
            {
                summarySheet.Cells[typeRow, 1].Value = GetSaleTypeDisplay(type.Key);
                summarySheet.Cells[typeRow, 2].Value = type.Value;
                typeRow++;
            }

            // Sales by Status
            summarySheet.Cells[9, 4].Value = _localizer["Sales by Status"];
            var statusRow = 10;
            foreach (var status in summary.SalesByStatus)
            {
                summarySheet.Cells[statusRow, 4].Value = status.Key;
                summarySheet.Cells[statusRow, 5].Value = status.Value;
                statusRow++;
            }

            // Sales by Payment Method
            summarySheet.Cells[9, 7].Value = _localizer["Sales by Payment Method"];
            summarySheet.Cells[9, 9].Value = _localizer["Amount"];
            var methodRow = 10;
            foreach (var method in summary.SalesByPaymentMethod)
            {
                summarySheet.Cells[methodRow, 7].Value = GetPaymentMethodDisplay(method.Key);
                summarySheet.Cells[methodRow, 8].Value = method.Value;

                // Add amount if available
                if (summary.SalesByPaymentMethodAmount.TryGetValue(method.Key, out var amount))
                {
                    summarySheet.Cells[methodRow, 9].Value = amount;
                    summarySheet.Cells[methodRow, 9].Style.Numberformat.Format = GetNumberFormat(culture);
                }

                methodRow++;
            }

            // Format the headers
            using (var range = summarySheet.Cells[1, 1, 1, 2])
            {
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 14;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            using (var range = summarySheet.Cells[9, 1, 9, 2])
            {
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            using (var range = summarySheet.Cells[9, 4, 9, 5])
            {
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            using (var range = summarySheet.Cells[9, 7, 9, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            // Add Sales by Date chart
            if (summary.SalesByDate.Any())
            {
                // Add data for chart
                var chartDataRow = Math.Max(typeRow, Math.Max(statusRow, methodRow)) + 2;
                summarySheet.Cells[chartDataRow, 1].Value = _localizer["Sales by Date"];
                summarySheet.Cells[chartDataRow, 1, chartDataRow, 3].Merge = true;
                summarySheet.Cells[chartDataRow, 1].Style.Font.Bold = true;

                summarySheet.Cells[chartDataRow + 1, 1].Value = _localizer["Date"];
                summarySheet.Cells[chartDataRow + 1, 2].Value = _localizer["Count"];
                summarySheet.Cells[chartDataRow + 1, 3].Value = _localizer["Total"];

                for (var i = 0; i < summary.SalesByDate.Count; i++)
                {
                    var sale = summary.SalesByDate[i];
                    // Formatear fecha usando el servicio DateTimeService
                    var saleDate = DateTime.SpecifyKind(sale.Date, DateTimeKind.Utc);
                    var formattedDate = _dateTime.FormatToTimezone(saleDate, timeZone);

                    summarySheet.Cells[chartDataRow + 2 + i, 1].Value = formattedDate;
                    summarySheet.Cells[chartDataRow + 2 + i, 2].Value = sale.Count;
                    summarySheet.Cells[chartDataRow + 2 + i, 3].Value = sale.Total;
                    summarySheet.Cells[chartDataRow + 2 + i, 3].Style.Numberformat.Format = GetNumberFormat(culture);
                }
            }

            // Create Details worksheet if there are any sales
            if (items.Any())
            {
                var productDetailsSheet = package.Workbook.Worksheets.Add(_localizer["Products"]);

                // Encabezados para la hoja de detalles de productos
                productDetailsSheet.Cells[1, 1].Value = _localizer["Sale #"];
                productDetailsSheet.Cells[1, 2].Value = _localizer["Date"];
                productDetailsSheet.Cells[1, 3].Value = _localizer["Customer"];
                productDetailsSheet.Cells[1, 4].Value = _localizer["Payment Method"];
                productDetailsSheet.Cells[1, 5].Value = _localizer["Product Code"];
                productDetailsSheet.Cells[1, 6].Value = _localizer["Product Name"];
                productDetailsSheet.Cells[1, 7].Value = _localizer["Quantity"];
                productDetailsSheet.Cells[1, 8].Value = _localizer["Unit Price"];
                productDetailsSheet.Cells[1, 9].Value = _localizer["Discount"];
                productDetailsSheet.Cells[1, 10].Value = _localizer["Tax"];
                productDetailsSheet.Cells[1, 11].Value = _localizer["Total"];

                // Formato para encabezados
                using (var range = productDetailsSheet.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Agregar datos de productos
                var row = 2;
                foreach (var sale in items)
                {
                    // Si la venta no tiene detalles, continuar con la siguiente
                    if (sale.Details == null || !sale.Details.Any())
                        continue;

                    foreach (var detail in sale.Details)
                    {
                        productDetailsSheet.Cells[row, 1].Value = sale.Id;
                        productDetailsSheet.Cells[row, 2].Value = _dateTime.FormatToTimezone(sale.SaleDate, timeZone);
                        productDetailsSheet.Cells[row, 3].Value = sale.CustomerName;
                        productDetailsSheet.Cells[row, 4].Value = GetPaymentMethodDisplay(
                            sale.Payments.FirstOrDefault()?.PaymentMethodName);
                        productDetailsSheet.Cells[row, 5].Value = detail.ProductCode;
                        productDetailsSheet.Cells[row, 6].Value = detail.ProductName;
                        productDetailsSheet.Cells[row, 7].Value = detail.Quantity;
                        productDetailsSheet.Cells[row, 8].Value = detail.UnitPrice;
                        productDetailsSheet.Cells[row, 9].Value = detail.DiscountAmount;
                        productDetailsSheet.Cells[row, 10].Value = detail.TaxAmount;
                        productDetailsSheet.Cells[row, 11].Value = detail.Total;

                        // Formato para columnas numéricas
                        productDetailsSheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";
                        productDetailsSheet.Cells[row, 8].Style.Numberformat.Format = GetNumberFormat(culture);
                        productDetailsSheet.Cells[row, 9].Style.Numberformat.Format = GetNumberFormat(culture);
                        productDetailsSheet.Cells[row, 10].Style.Numberformat.Format = GetNumberFormat(culture);
                        productDetailsSheet.Cells[row, 11].Style.Numberformat.Format = GetNumberFormat(culture);

                        row++;
                    }
                }

                // Auto-ajustar columnas
                productDetailsSheet.Cells.AutoFitColumns();

                // Agregar formato de tabla para facilitar el filtrado
                var tableRange = productDetailsSheet.Cells[1, 1, row - 1, 11];
                var table = productDetailsSheet.Tables.Add(tableRange, "ProductsTable");
                table.ShowHeader = true;
                table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
            }

            return await package.GetAsByteArrayAsync(cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportSalesReportToPdfAsync(
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar límite de registros para exportación PDF
            if (request.PageSize > _exportOptions.MaxPdfRecords)
            {
                throw new InvalidOperationException(
                    $"PDF request exceeds maximum allowed records ({_exportOptions.MaxPdfRecords})");
            }

            // Get report data
            var (_, items) = await GetSalesReportAsync(request, cancellationToken);
            var summary = await GetSalesSummaryAsync(request, cancellationToken);

            string logoBase64 = string.Empty;
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.webp");

            if (File.Exists(logoPath))
            {
                byte[] logoBytes = await File.ReadAllBytesAsync(logoPath, cancellationToken);
                logoBase64 = Convert.ToBase64String(logoBytes);
            }

            // Prepare the model for the PDF view
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;
            var reportData = new SalesReportPdfDto
            {
                Summary = summary,
                Sales = items,
                TimeZone = timeZone,
                GeneratedAt = _dateTime.Now,
                LogoBase64 = logoBase64
            };

            // Generate the PDF from the view
            var content = await _pdfService.GeneratePdfFromViewAsync(
                "/Views/Reports/Sales/SalesReport.cshtml",
                reportData,
                cancellationToken);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report to PDF");
            throw;
        }
    }

    public async Task<(byte[] Content, string FileName)> ExportSalesHistoryToExcelAsync(
        SalesReportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                throw new InvalidOperationException(
                    $"Export request exceeds maximum allowed records ({_exportOptions.MaxExportRecords})");
            }

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var baseQuery = await GetBaseQueryAsync(context, request, cancellationToken);

            var salesQuery = baseQuery
                .OrderByDescending(s => s.SaleDate)
                .Include(s => s.Customer)
                .Include(s => s.Location)
                .Include(s => s.Quotation)
                .Include(s => s.Details).ThenInclude(d => d.Product)
                .Take(request.PageSize);

            var saleEntities = await salesQuery.ToListAsync(cancellationToken);
            var items = saleEntities.Select(s => _mapper.Map<SaleDto>(s)).ToList();

            var saleIds = saleEntities.Select(s => s.Id).ToList();
            var latestInvoiceIds = await context.Set<MexicoInvoice>()
                .Where(i => saleIds.Contains(i.SaleId))
                .GroupBy(i => i.SaleId)
                .Select(g => g.Max(i => i.Id))
                .ToListAsync(cancellationToken);

            var invoicesBySaleId = await context.Set<MexicoInvoice>()
                .Where(i => latestInvoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.SaleId, cancellationToken);

            var remissionsBySaleId = (await context.Set<Remission>()
                .Where(r => r.ConsolidatedSaleId != null && saleIds.Contains(r.ConsolidatedSaleId!.Value))
                .Select(r => new { r.ConsolidatedSaleId, r.RemissionNumber })
                .ToListAsync(cancellationToken))
                .GroupBy(r => r.ConsolidatedSaleId!.Value)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(r => r.RemissionNumber)));

            var globalInvoicesBySaleId = await context.GlobalInvoiceSales
                .AsNoTracking()
                .Where(gs => saleIds.Contains(gs.SaleId) && gs.GlobalInvoice!.Status == App.Core.Enums.Billing.GlobalInvoiceStatus.Stamped)
                .Select(gs => new { gs.SaleId, gs.GlobalInvoice!.Serie, gs.GlobalInvoice!.Folio, gs.GlobalInvoice!.Uuid })
                .ToDictionaryAsync(gs => gs.SaleId, cancellationToken);

            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;
            var numberFormat = GetNumberFormat(culture);
            var percentFormat = "0.##\\%";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add(_localizer["Sales History"]);

            // Headers
            var headers = new[]
            {
                _localizer["Sale #"].Value,
                _localizer["Date"].Value,
                _localizer["Customer"].Value,
                _localizer["Payment Method"].Value,
                _localizer["Amount"].Value,
                _localizer["Discount"].Value,
                _localizer["Subtotal"].Value,
                _localizer["Tax Rate"].Value,
                _localizer["Tax"].Value,
                _localizer["Total"].Value,
                _localizer["Location"].Value,
                _localizer["Quotation"].Value,
                _localizer["Created By"].Value,
                _localizer["Status"].Value,
                _localizer["Has Invoice"].Value,
                _localizer["CFDI Serie"].Value,
                _localizer["CFDI Folio"].Value,
                _localizer["UUID / Folio Fiscal"].Value,
                _localizer["Customer RFC"].Value,
                _localizer["Customer Legal Name"].Value,
                _localizer["CFDI Status"].Value,
                _localizer["Stamp Date"].Value,
                _localizer["Cancellation Reason"].Value,
                _localizer["Replacement UUID"].Value,
                _localizer["Remissions"].Value,
                _localizer["Has Global Invoice"].Value,
                _localizer["Global Invoice Folio"].Value,
                _localizer["Global Invoice UUID"].Value
            };

            for (int col = 1; col <= headers.Length; col++)
                ws.Cells[1, col].Value = headers[col - 1];

            using (var headerRange = ws.Cells[1, 1, 1, headers.Length])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 121, 107)); // teal
                headerRange.Style.Font.Color.SetColor(Color.White);
                headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // Data rows
            int row = 2;
            foreach (var s in items)
            {
                var paymentMethod = s.Payments.FirstOrDefault()?.PaymentMethodName ?? string.Empty;
                var subtotalAfterDiscount = s.Subtotal - s.DiscountAmount;
                var dateStr = _dateTime.FormatToTimezone(s.SaleDate, timeZone);
                var statusStr = s.Status == App.Core.Enums.Shop.SaleStatus.Created
                    ? _localizer["Created"].Value
                    : _localizer["Cancelled"].Value;

                invoicesBySaleId.TryGetValue(s.Id, out var invoice);
                remissionsBySaleId.TryGetValue(s.Id, out var remissionNumbers);
                globalInvoicesBySaleId.TryGetValue(s.Id, out var globalInvoice);

                ws.Cells[row, 1].Value = s.Id;
                ws.Cells[row, 2].Value = dateStr;
                ws.Cells[row, 3].Value = s.CustomerName;
                ws.Cells[row, 4].Value = paymentMethod;
                ws.Cells[row, 5].Value = s.Subtotal;
                ws.Cells[row, 5].Style.Numberformat.Format = numberFormat;
                ws.Cells[row, 6].Value = s.DiscountAmount > 0 ? s.DiscountAmount : (object)string.Empty;
                if (s.DiscountAmount > 0)
                    ws.Cells[row, 6].Style.Numberformat.Format = numberFormat;
                ws.Cells[row, 7].Value = subtotalAfterDiscount;
                ws.Cells[row, 7].Style.Numberformat.Format = numberFormat;
                ws.Cells[row, 8].Value = s.TaxRate * 100;
                ws.Cells[row, 8].Style.Numberformat.Format = percentFormat;
                ws.Cells[row, 9].Value = s.TaxAmount;
                ws.Cells[row, 9].Style.Numberformat.Format = numberFormat;
                ws.Cells[row, 10].Value = s.Total;
                ws.Cells[row, 10].Style.Numberformat.Format = numberFormat;
                ws.Cells[row, 11].Value = s.LocationName ?? string.Empty;
                ws.Cells[row, 12].Value = s.QuotationNumber ?? string.Empty;
                ws.Cells[row, 13].Value = s.CreatedBy ?? string.Empty;
                ws.Cells[row, 14].Value = statusStr;

                // Invoice columns (15-24)
                ws.Cells[row, 15].Value = invoice != null ? _localizer["Yes"].Value : _localizer["No"].Value;
                ws.Cells[row, 16].Value = invoice?.Serie ?? string.Empty;
                ws.Cells[row, 17].Value = invoice != null ? invoice.Folio : (object)string.Empty;
                ws.Cells[row, 18].Value = invoice?.Uuid ?? string.Empty;
                ws.Cells[row, 19].Value = invoice?.CustomerRfc ?? string.Empty;
                ws.Cells[row, 20].Value = invoice?.CustomerLegalName ?? string.Empty;
                ws.Cells[row, 21].Value = invoice != null ? TranslateCfdiStatus(invoice.Status) : string.Empty;
                ws.Cells[row, 22].Value = invoice?.StampDate.HasValue == true
                    ? _dateTime.FormatToTimezone(invoice.StampDate.Value, timeZone)
                    : string.Empty;
                ws.Cells[row, 23].Value = invoice?.CancellationReason != null
                    ? FormatCancellationReason(invoice.CancellationReason)
                    : string.Empty;
                ws.Cells[row, 24].Value = invoice?.ReplacementUuid ?? string.Empty;
                ws.Cells[row, 25].Value = remissionNumbers ?? string.Empty;

                // Global invoice columns (26-28)
                ws.Cells[row, 26].Value = globalInvoice != null ? _localizer["Yes"].Value : _localizer["No"].Value;
                ws.Cells[row, 27].Value = globalInvoice != null ? $"{globalInvoice.Serie}-{globalInvoice.Folio}" : string.Empty;
                ws.Cells[row, 28].Value = globalInvoice?.Uuid ?? string.Empty;

                // Striped rows
                if (row % 2 == 0)
                {
                    using var rowRange = ws.Cells[row, 1, row, headers.Length];
                    rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245, 245, 245));
                }

                row++;
            }

            // Auto-fit and table
            ws.Cells.AutoFitColumns();
            if (items.Count > 0)
            {
                var tableRange = ws.Cells[1, 1, row - 1, headers.Length];
                var table = ws.Tables.Add(tableRange, "SalesHistoryTable");
                table.ShowHeader = true;
                table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
            }

            var startStr = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var endStr = request.EndDate?.ToString("yyyyMMdd") ?? "today";
            var fileName = $"sales_history_{startStr}_to_{endStr}.xlsx";

            return (await package.GetAsByteArrayAsync(cancellationToken), fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales history to Excel");
            throw;
        }
    }

    #region Helper Methods

    private string TranslateCfdiStatus(string status) => status switch
    {
        "Stamped" => _localizer["Stamped"].Value,
        "Cancelled" => _localizer["Cancelled"].Value,
        "Draft" => _localizer["Draft"].Value,
        "StampError" => _localizer["Stamp Error"].Value,
        _ => status
    };

    private static string FormatCancellationReason(string reason) => reason switch
    {
        "01" => "01 - Comprobante emitido con errores con relación",
        "02" => "02 - Comprobante emitido con errores sin relación",
        "03" => "03 - No se llevó a cabo la operación",
        "04" => "04 - Operación nominativa relacionada en factura global",
        _ => reason
    };

    private async Task<IQueryable<Sale>> GetBaseQueryAsync(
        ApplicationDbContext context,
        SalesReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sale> query = context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod);

        // Obtener la zona horaria actual
        var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

        // Aplicar filtros de fecha convirtiendo a UTC
        if (request.StartDate.HasValue)
        {
            var utcStart = _dateTime.ToUtc(request.StartDate.Value.Date, timeZone);
            query = query.Where(s => s.SaleDate >= utcStart);
        }

        if (request.EndDate.HasValue)
        {
            var utcEnd = _dateTime.ToUtc(request.EndDate.Value.Date.AddDays(1).AddTicks(-1), timeZone);
            query = query.Where(s => s.SaleDate <= utcEnd);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchString))
        {
            query = query.Where(s =>
                s.Customer.Name.Contains(request.SearchString) ||
                s.Id.ToString().Contains(request.SearchString));
        }

        // Apply customer filter
        if (request.CustomerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<App.Core.Enums.Shop.SaleStatus>(request.Status, out var statusEnum))
        {
            query = query.Where(s => s.Status == statusEnum);
        }

        // Apply sale type filter
        if (request.SaleType.HasValue)
        {
            query = query.Where(s => s.SaleType == request.SaleType.Value);
        }

        // Apply payment method filter
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            query = query.Where(s => s.Payments.Any(p => p.PaymentMethod.Name == request.PaymentMethod));
        }

        // Apply location filter
        if (request.LocationId.HasValue)
        {
            query = query.Where(s => s.LocationId == request.LocationId.Value);
        }

        return query;
    }

    private string GetNumberFormat(CultureInfo culture)
    {
        var numberFormat = culture.NumberFormat;
        var decimalSeparator = numberFormat.NumberDecimalSeparator;
        var groupSeparator = numberFormat.NumberGroupSeparator;

        return $"#{groupSeparator}##0{decimalSeparator}00";
    }

    private string GetSaleTypeDisplay(string saleType)
    {
        return saleType switch
        {
            "Public" => _localizer["Public"],
            _ => saleType
        };
    }

    private string GetPaymentMethodDisplay(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return string.Empty;

        // Try to localize the payment method
        // This handles both English format (Cash, CreditCard) and Spanish format (Efectivo, Tarjeta de Crédito)
        return _localizer[paymentMethod];
    }

    #endregion
}