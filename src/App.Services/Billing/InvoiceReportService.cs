using System.Drawing;
using System.Globalization;

using App.Core.DTOs.Billing;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Interfaces.Billing;
using App.Models.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace App.Services.Billing;

public class InvoiceReportService : IInvoiceReportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IStringLocalizer<InvoiceReportService> _localizer;
    private readonly ILogger<InvoiceReportService> _logger;

    private static readonly Color HeaderColor = Color.FromArgb(0, 121, 107);
    private static readonly Color HeaderTextColor = Color.White;
    private static readonly Color StripeColor = Color.FromArgb(245, 245, 245);
    private static readonly Color NoCoverageColor = Color.FromArgb(255, 235, 238);
    private static readonly Color CoverageColor = Color.FromArgb(232, 245, 233);
    private static readonly Color TotalColor = Color.FromArgb(0, 96, 100);
    private static readonly Color StampErrorColor = Color.FromArgb(255, 243, 224);
    private static readonly Color CancelledTotalColor = Color.FromArgb(183, 28, 28);
    private static readonly Color StampErrorTotalColor = Color.FromArgb(230, 81, 0);

    public InvoiceReportService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IStringLocalizer<InvoiceReportService> localizer,
        ILogger<InvoiceReportService> logger)
    {
        _contextFactory = contextFactory;
        _localizer = localizer;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Report 1 — Individual CFDIs
    // ─────────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportIndividualInvoicesAsync(
        InvoiceReportRequestDto request,
        CultureInfo culture,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.MexicoInvoices
            .Include(i => i.Sale)
            .AsNoTracking();

        if (request.StartDate.HasValue)
            query = query.Where(i => i.StampDate >= request.StartDate.Value ||
                                     (i.StampDate == null && i.RequestedInvoiceDate >= request.StartDate.Value));

        if (request.EndDate.HasValue)
        {
            var endInclusive = request.EndDate.Value.AddDays(1);
            query = query.Where(i => i.StampDate < endInclusive ||
                                     (i.StampDate == null && i.RequestedInvoiceDate < endInclusive));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerRfc))
            query = query.Where(i => i.CustomerRfc.Contains(request.CustomerRfc));

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
            query = query.Where(i => i.Status == request.Status);

        var invoices = await query
            .OrderBy(i => i.StampDate ?? i.RequestedInvoiceDate)
            .Take(request.PageSize)
            .ToListAsync(ct);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add(_localizer["Individual Invoices"].Value);
        var fmt = GetNumberFormat(culture);

        var headers = new[]
        {
            _localizer["UUID (Folio Fiscal)"].Value,
            _localizer["Serie"].Value,
            _localizer["Folio"].Value,
            _localizer["Stamp Date"].Value,
            _localizer["Sale Date"].Value,
            _localizer["Sale #"].Value,
            _localizer["Customer RFC"].Value,
            _localizer["Customer Legal Name"].Value,
            _localizer["Customer Postal Code"].Value,
            _localizer["Customer Fiscal Regime"].Value,
            _localizer["CFDI Use"].Value,
            _localizer["Subtotal"].Value,
            _localizer["Discount"].Value,
            _localizer["Tax Amount"].Value,
            _localizer["Total"].Value,
            _localizer["Payment Method"].Value,
            _localizer["Payment Form"].Value,
            _localizer["Currency"].Value,
            _localizer["Status"].Value,
            _localizer["Cancellation Date"].Value,
            _localizer["Cancellation Reason"].Value
        };

        WriteHeader(ws, headers);

        int row = 2;
        foreach (var inv in invoices)
        {
            ws.Cells[row, 1].Value = inv.Uuid ?? string.Empty;
            ws.Cells[row, 2].Value = inv.Serie ?? string.Empty;
            ws.Cells[row, 3].Value = inv.Folio;
            ws.Cells[row, 4].Value = inv.StampDate.HasValue ? inv.StampDate.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty;
            ws.Cells[row, 5].Value = inv.Sale != null ? inv.Sale.SaleDate.ToString("yyyy-MM-dd") : string.Empty;
            ws.Cells[row, 6].Value = inv.SaleId;
            ws.Cells[row, 7].Value = inv.CustomerRfc;
            ws.Cells[row, 8].Value = inv.CustomerLegalName;
            ws.Cells[row, 9].Value = inv.CustomerPostalCode;
            ws.Cells[row, 10].Value = inv.CustomerFiscalRegime;
            ws.Cells[row, 11].Value = inv.CfdiUse;
            ws.Cells[row, 12].Value = inv.Subtotal;
            ws.Cells[row, 12].Style.Numberformat.Format = fmt;
            ws.Cells[row, 13].Value = inv.Sale?.DiscountAmount ?? 0;
            ws.Cells[row, 13].Style.Numberformat.Format = fmt;
            ws.Cells[row, 14].Value = inv.TaxAmount;
            ws.Cells[row, 14].Style.Numberformat.Format = fmt;
            ws.Cells[row, 15].Value = inv.Total;
            ws.Cells[row, 15].Style.Numberformat.Format = fmt;
            ws.Cells[row, 16].Value = inv.PaymentMethod;
            ws.Cells[row, 17].Value = inv.PaymentForm;
            ws.Cells[row, 18].Value = inv.Currency;
            ws.Cells[row, 19].Value = TranslateInvoiceStatus(inv.Status);
            ws.Cells[row, 20].Value = inv.CancellationDate.HasValue ? inv.CancellationDate.Value.ToString("yyyy-MM-dd") : string.Empty;
            ws.Cells[row, 21].Value = inv.CancellationReason ?? string.Empty;

            if (row % 2 == 0)
                ApplyStripe(ws, row, headers.Length);

            row++;
        }

        FinalizeSheet(ws, headers.Length, row);
        return await package.GetAsByteArrayAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Report 2 — Global Invoices
    // ─────────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportGlobalInvoicesAsync(
        InvoiceReportRequestDto request,
        CultureInfo culture,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.GlobalInvoices
            .Include(g => g.GlobalInvoiceSales)
                .ThenInclude(gis => gis.Sale)
            .AsNoTracking();

        if (request.StartDate.HasValue)
            query = query.Where(g => g.EndDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(g => g.StartDate <= request.EndDate.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
        {
            if (Enum.TryParse<GlobalInvoiceStatus>(request.Status, true, out var statusEnum))
                query = query.Where(g => g.Status == statusEnum);
        }

        var globals = await query
            .OrderBy(g => g.StampDate ?? g.CreatedAt)
            .Take(request.PageSize)
            .ToListAsync(ct);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var fmt = GetNumberFormat(culture);

        // Sheet 1 — Summary per global invoice
        var ws1 = package.Workbook.Worksheets.Add(_localizer["Global Invoices"].Value);
        var headers1 = new[]
        {
            _localizer["UUID (Folio Fiscal)"].Value,
            _localizer["Serie"].Value,
            _localizer["Folio"].Value,
            _localizer["Stamp Date"].Value,
            _localizer["Periodicity"].Value,
            _localizer["Period Month"].Value,
            _localizer["Period Year"].Value,
            _localizer["Issuer RFC"].Value,
            _localizer["Issuer Legal Name"].Value,
            _localizer["Sale Count"].Value,
            _localizer["Subtotal"].Value,
            _localizer["Tax Amount"].Value,
            _localizer["Total"].Value,
            _localizer["Status"].Value,
            _localizer["Cancellation Date"].Value
        };
        WriteHeader(ws1, headers1);

        int row1 = 2;
        foreach (var g in globals)
        {
            ws1.Cells[row1, 1].Value = g.Uuid ?? string.Empty;
            ws1.Cells[row1, 2].Value = g.Serie ?? string.Empty;
            ws1.Cells[row1, 3].Value = g.Folio;
            ws1.Cells[row1, 4].Value = g.StampDate.HasValue ? g.StampDate.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty;
            ws1.Cells[row1, 5].Value = TranslatePeriodicity(g.Periodicity);
            ws1.Cells[row1, 6].Value = g.PeriodMonth;
            ws1.Cells[row1, 7].Value = g.PeriodYear;
            ws1.Cells[row1, 8].Value = g.IssuerRfc;
            ws1.Cells[row1, 9].Value = g.IssuerLegalName;
            ws1.Cells[row1, 10].Value = g.SaleCount;
            ws1.Cells[row1, 11].Value = g.Subtotal;
            ws1.Cells[row1, 11].Style.Numberformat.Format = fmt;
            ws1.Cells[row1, 12].Value = g.TaxAmount;
            ws1.Cells[row1, 12].Style.Numberformat.Format = fmt;
            ws1.Cells[row1, 13].Value = g.Total;
            ws1.Cells[row1, 13].Style.Numberformat.Format = fmt;
            ws1.Cells[row1, 14].Value = TranslateGlobalStatus(g.Status);
            ws1.Cells[row1, 15].Value = g.CancellationDate.HasValue ? g.CancellationDate.Value.ToString("yyyy-MM-dd") : string.Empty;

            if (row1 % 2 == 0)
                ApplyStripe(ws1, row1, headers1.Length);

            row1++;
        }
        FinalizeSheet(ws1, headers1.Length, row1);

        // Sheet 2 — Sales included in each global invoice
        var ws2 = package.Workbook.Worksheets.Add(_localizer["Included Sales"].Value);
        var headers2 = new[]
        {
            _localizer["UUID (Folio Fiscal)"].Value,
            _localizer["Global Invoice Folio"].Value,
            _localizer["Sale #"].Value,
            _localizer["Sale Date"].Value,
            _localizer["Subtotal"].Value,
            _localizer["Tax Amount"].Value,
            _localizer["Total"].Value
        };
        WriteHeader(ws2, headers2);

        int row2 = 2;
        foreach (var g in globals)
        {
            foreach (var gis in g.GlobalInvoiceSales)
            {
                ws2.Cells[row2, 1].Value = g.Uuid ?? string.Empty;
                ws2.Cells[row2, 2].Value = string.IsNullOrEmpty(g.Serie) ? g.Folio.ToString() : $"{g.Serie}-{g.Folio}";
                ws2.Cells[row2, 3].Value = gis.SaleId;
                ws2.Cells[row2, 4].Value = gis.Sale?.SaleDate.ToString("yyyy-MM-dd") ?? string.Empty;
                ws2.Cells[row2, 5].Value = gis.Sale?.Subtotal ?? 0;
                ws2.Cells[row2, 5].Style.Numberformat.Format = fmt;
                ws2.Cells[row2, 6].Value = gis.Sale?.TaxAmount ?? 0;
                ws2.Cells[row2, 6].Style.Numberformat.Format = fmt;
                ws2.Cells[row2, 7].Value = gis.Sale?.Total ?? 0;
                ws2.Cells[row2, 7].Style.Numberformat.Format = fmt;

                if (row2 % 2 == 0)
                    ApplyStripe(ws2, row2, headers2.Length);

                row2++;
            }
        }
        FinalizeSheet(ws2, headers2.Length, row2);

        return await package.GetAsByteArrayAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Report 3 — Conciliation
    // ─────────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportConciliationAsync(
        InvoiceReportRequestDto request,
        CultureInfo culture,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = request.EndDate?.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);

        var sales = await context.Sales
            .Include(s => s.Customer)
            .Where(s => s.SaleDate >= startDate && s.SaleDate < endDate && s.Status != SaleStatus.Cancelled)
            .OrderBy(s => s.SaleDate)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        var saleIds = sales.Select(s => s.Id).ToHashSet();

        var invoices = await context.MexicoInvoices
            .Where(i => saleIds.Contains(i.SaleId))
            .AsNoTracking()
            .ToListAsync(ct);
        var invoicesBySaleId = invoices.ToDictionary(i => i.SaleId);

        var globalInvoiceSales = await context.GlobalInvoiceSales
            .Include(gis => gis.GlobalInvoice)
            .Where(gis => saleIds.Contains(gis.SaleId))
            .AsNoTracking()
            .ToListAsync(ct);
        var globalInvoiceBySaleId = globalInvoiceSales
            .GroupBy(gis => gis.SaleId)
            .ToDictionary(g => g.Key, g => g.Select(gis => gis.GlobalInvoice).OrderByDescending(gi => gi.Status == GlobalInvoiceStatus.Stamped).First());

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var fmt = GetNumberFormat(culture);

        // Detail sheet
        var wsDetail = package.Workbook.Worksheets.Add(_localizer["Conciliation"].Value);
        var headers = new[]
        {
            _localizer["Sale #"].Value,
            _localizer["Sale Date"].Value,
            _localizer["Customer"].Value,
            _localizer["Sale Type"].Value,
            _localizer["Subtotal"].Value,
            _localizer["Tax Amount"].Value,
            _localizer["Total"].Value,
            _localizer["Has Individual Invoice"].Value,
            _localizer["UUID Individual CFDI"].Value,
            _localizer["CFDI Status"].Value,
            _localizer["In Global Invoice"].Value,
            _localizer["UUID Global Invoice"].Value,
            _localizer["Global Invoice Status"].Value,
            _localizer["Coverage"].Value
        };
        WriteHeader(wsDetail, headers);

        int row = 2;
        decimal totalSales = 0, totalCoveredIndividual = 0, totalCoveredGlobal = 0, totalNoCoverage = 0;
        int countCoveredIndividual = 0, countCoveredGlobal = 0, countNoCoverage = 0;

        foreach (var sale in sales)
        {
            invoicesBySaleId.TryGetValue(sale.Id, out var inv);
            globalInvoiceBySaleId.TryGetValue(sale.Id, out var globalInv);

            bool hasActiveIndividual = inv != null && inv.IsStamped && inv.Status == "Stamped";
            bool hasActiveGlobal = globalInv != null && globalInv.Status == GlobalInvoiceStatus.Stamped;

            string coverage;
            if (hasActiveIndividual)
                coverage = _localizer["Individual"].Value;
            else if (hasActiveGlobal)
                coverage = _localizer["Global"].Value;
            else
                coverage = _localizer["No Coverage"].Value;

            wsDetail.Cells[row, 1].Value = sale.Id;
            wsDetail.Cells[row, 2].Value = sale.SaleDate.ToString("yyyy-MM-dd");
            wsDetail.Cells[row, 3].Value = sale.Customer?.Name ?? string.Empty;
            wsDetail.Cells[row, 4].Value = sale.SaleType.ToString();
            wsDetail.Cells[row, 5].Value = sale.Subtotal;
            wsDetail.Cells[row, 5].Style.Numberformat.Format = fmt;
            wsDetail.Cells[row, 6].Value = sale.TaxAmount;
            wsDetail.Cells[row, 6].Style.Numberformat.Format = fmt;
            wsDetail.Cells[row, 7].Value = sale.Total;
            wsDetail.Cells[row, 7].Style.Numberformat.Format = fmt;
            wsDetail.Cells[row, 8].Value = inv != null ? _localizer["Yes"].Value : _localizer["No"].Value;
            wsDetail.Cells[row, 9].Value = inv?.Uuid ?? string.Empty;
            wsDetail.Cells[row, 10].Value = inv != null ? TranslateInvoiceStatus(inv.Status) : string.Empty;
            wsDetail.Cells[row, 11].Value = globalInv != null ? _localizer["Yes"].Value : _localizer["No"].Value;
            wsDetail.Cells[row, 12].Value = globalInv?.Uuid ?? string.Empty;
            wsDetail.Cells[row, 13].Value = globalInv != null ? TranslateGlobalStatus(globalInv.Status) : string.Empty;
            wsDetail.Cells[row, 14].Value = coverage;

            // Color coding
            if (coverage == _localizer["No Coverage"].Value)
            {
                using var rangeNoCov = wsDetail.Cells[row, 1, row, headers.Length];
                rangeNoCov.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rangeNoCov.Style.Fill.BackgroundColor.SetColor(NoCoverageColor);
                totalNoCoverage += sale.Total;
                countNoCoverage++;
            }
            else
            {
                using var rangeCov = wsDetail.Cells[row, 1, row, headers.Length];
                rangeCov.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rangeCov.Style.Fill.BackgroundColor.SetColor(CoverageColor);
                if (hasActiveIndividual) { totalCoveredIndividual += sale.Total; countCoveredIndividual++; }
                else { totalCoveredGlobal += sale.Total; countCoveredGlobal++; }
            }

            totalSales += sale.Total;
            row++;
        }

        FinalizeSheet(wsDetail, headers.Length, row);

        // Summary sheet
        var wsSummary = package.Workbook.Worksheets.Add(_localizer["Conciliation Summary"].Value);
        package.Workbook.Worksheets.MoveToStart(_localizer["Conciliation Summary"].Value);

        WriteSummaryRow(wsSummary, 1, _localizer["Period"].Value,
            $"{request.StartDate?.ToString("yyyy-MM-dd")} — {request.EndDate?.ToString("yyyy-MM-dd")}");
        WriteSummaryRow(wsSummary, 2, _localizer["Total Sales"].Value, sales.Count.ToString());
        WriteSummaryRow(wsSummary, 3, _localizer["Total Revenue"].Value, totalSales.ToString(fmt, culture));
        wsSummary.Cells[4, 1].Value = string.Empty;
        WriteSummaryRow(wsSummary, 5, _localizer["Covered by Individual CFDI"].Value, $"{countCoveredIndividual} ({totalCoveredIndividual.ToString(fmt, culture)})");
        WriteSummaryRow(wsSummary, 6, _localizer["Covered by Global Invoice"].Value, $"{countCoveredGlobal} ({totalCoveredGlobal.ToString(fmt, culture)})");
        WriteSummaryRow(wsSummary, 7, _localizer["Without Tax Coverage"].Value, $"{countNoCoverage} ({totalNoCoverage.ToString(fmt, culture)})");
        double coveragePercent = sales.Count > 0 ? (double)(sales.Count - countNoCoverage) / sales.Count * 100 : 0;
        WriteSummaryRow(wsSummary, 8, _localizer["Fiscal Coverage"].Value, $"{coveragePercent:F1}%");

        using (var headerRange = wsSummary.Cells[1, 1, 8, 2])
        {
            headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        if (countNoCoverage > 0)
        {
            using var alertRange = wsSummary.Cells[7, 1, 7, 2];
            alertRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            alertRange.Style.Fill.BackgroundColor.SetColor(NoCoverageColor);
            alertRange.Style.Font.Bold = true;
        }

        wsSummary.Column(1).Width = 35;
        wsSummary.Column(2).Width = 40;

        return await package.GetAsByteArrayAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Report 4 — VAT Report
    // ─────────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportVatReportAsync(
        InvoiceReportRequestDto request,
        CultureInfo culture,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-12);
        var endDate = request.EndDate?.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);

        var individualInvoices = await context.MexicoInvoices
            .Include(i => i.Sale)
            .Where(i => (i.StampDate >= startDate && i.StampDate < endDate) ||
                        (i.StampDate == null && i.RequestedInvoiceDate >= startDate && i.RequestedInvoiceDate < endDate))
            .AsNoTracking()
            .ToListAsync(ct);

        var globalInvoices = await context.GlobalInvoices
            .Where(g => g.StampDate >= startDate && g.StampDate < endDate)
            .AsNoTracking()
            .ToListAsync(ct);

        // Group by year-month
        var individualByMonth = individualInvoices
            .Where(i => i.IsStamped)
            .GroupBy(i => (i.StampDate!.Value.Year, i.StampDate!.Value.Month))
            .ToDictionary(g => g.Key, g => g.ToList());

        var cancelledByMonth = individualInvoices
            .Where(i => i.CancellationDate.HasValue)
            .GroupBy(i => (i.CancellationDate!.Value.Year, i.CancellationDate!.Value.Month))
            .ToDictionary(g => g.Key, g => g.ToList());

        var globalByMonth = globalInvoices
            .Where(g => g.Status == GlobalInvoiceStatus.Stamped)
            .GroupBy(g => (g.StampDate!.Value.Year, g.StampDate!.Value.Month))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Collect all months in range
        var allMonths = new HashSet<(int Year, int Month)>();
        foreach (var k in individualByMonth.Keys) allMonths.Add(k);
        foreach (var k in globalByMonth.Keys) allMonths.Add(k);
        var sortedMonths = allMonths.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var fmt = GetNumberFormat(culture);

        var ws = package.Workbook.Worksheets.Add(_localizer["VAT Report"].Value);
        var headers = new[]
        {
            _localizer["Period"].Value,
            _localizer["Individual CFDIs Issued"].Value,
            _localizer["Global CFDIs Issued"].Value,
            _localizer["Subtotal"].Value,
            _localizer["VAT 16%"].Value,
            _localizer["VAT 8% (Border)"].Value,
            _localizer["Total VAT"].Value,
            _localizer["Total Invoiced"].Value,
            _localizer["Cancelled CFDIs"].Value,
            _localizer["VAT Cancelled (Negative)"].Value
        };
        WriteHeader(ws, headers);

        int row = 2;
        decimal totSubtotal = 0, totVat16 = 0, totVat8 = 0, totTotal = 0, totVatCancelled = 0;
        int totIndividual = 0, totGlobal = 0, totCancelled = 0;

        foreach (var month in sortedMonths)
        {
            var indList = individualByMonth.TryGetValue(month, out var il) ? il : new();
            var globList = globalByMonth.TryGetValue(month, out var gl) ? gl : new();
            var cancList = cancelledByMonth.TryGetValue(month, out var cl) ? cl : new();

            decimal subtotal = indList.Sum(i => i.Subtotal) + globList.Sum(g => g.Subtotal);
            decimal vatCancelled = cancList.Sum(i => i.TaxAmount);

            // Determine 16% vs 8% by rate approximation from each individual invoice
            decimal vat16 = indList.Where(i => i.Subtotal > 0 && i.TaxAmount / i.Subtotal >= 0.12m).Sum(i => i.TaxAmount)
                           + globList.Where(g => g.Subtotal > 0 && g.TaxAmount / g.Subtotal >= 0.12m).Sum(g => g.TaxAmount);
            decimal vat8 = indList.Where(i => i.Subtotal > 0 && i.TaxAmount / i.Subtotal < 0.12m && i.TaxAmount > 0).Sum(i => i.TaxAmount)
                          + globList.Where(g => g.Subtotal > 0 && g.TaxAmount / g.Subtotal < 0.12m && g.TaxAmount > 0).Sum(g => g.TaxAmount);
            decimal totalVat = vat16 + vat8;
            decimal totalInvoiced = indList.Sum(i => i.Total) + globList.Sum(g => g.Total);

            var periodLabel = new DateTime(month.Year, month.Month, 1).ToString("MMM yyyy", culture);
            ws.Cells[row, 1].Value = periodLabel;
            ws.Cells[row, 2].Value = indList.Count;
            ws.Cells[row, 3].Value = globList.Count;
            ws.Cells[row, 4].Value = subtotal;
            ws.Cells[row, 4].Style.Numberformat.Format = fmt;
            ws.Cells[row, 5].Value = vat16;
            ws.Cells[row, 5].Style.Numberformat.Format = fmt;
            ws.Cells[row, 6].Value = vat8;
            ws.Cells[row, 6].Style.Numberformat.Format = fmt;
            ws.Cells[row, 7].Value = totalVat;
            ws.Cells[row, 7].Style.Numberformat.Format = fmt;
            ws.Cells[row, 8].Value = totalInvoiced;
            ws.Cells[row, 8].Style.Numberformat.Format = fmt;
            ws.Cells[row, 9].Value = cancList.Count;
            ws.Cells[row, 10].Value = vatCancelled > 0 ? -vatCancelled : (object)string.Empty;
            if (vatCancelled > 0)
                ws.Cells[row, 10].Style.Numberformat.Format = fmt;

            if (row % 2 == 0)
                ApplyStripe(ws, row, headers.Length);

            totSubtotal += subtotal;
            totVat16 += vat16;
            totVat8 += vat8;
            totTotal += totalInvoiced;
            totVatCancelled += vatCancelled;
            totIndividual += indList.Count;
            totGlobal += globList.Count;
            totCancelled += cancList.Count;
            row++;
        }

        // Totals row
        using (var totalRange = ws.Cells[row, 1, row, headers.Length])
        {
            totalRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            totalRange.Style.Fill.BackgroundColor.SetColor(TotalColor);
            totalRange.Style.Font.Color.SetColor(Color.White);
            totalRange.Style.Font.Bold = true;
        }
        ws.Cells[row, 1].Value = _localizer["Total"].Value;
        ws.Cells[row, 2].Value = totIndividual;
        ws.Cells[row, 3].Value = totGlobal;
        ws.Cells[row, 4].Value = totSubtotal;
        ws.Cells[row, 4].Style.Numberformat.Format = fmt;
        ws.Cells[row, 5].Value = totVat16;
        ws.Cells[row, 5].Style.Numberformat.Format = fmt;
        ws.Cells[row, 6].Value = totVat8;
        ws.Cells[row, 6].Style.Numberformat.Format = fmt;
        ws.Cells[row, 7].Value = totVat16 + totVat8;
        ws.Cells[row, 7].Style.Numberformat.Format = fmt;
        ws.Cells[row, 8].Value = totTotal;
        ws.Cells[row, 8].Style.Numberformat.Format = fmt;
        ws.Cells[row, 9].Value = totCancelled;
        ws.Cells[row, 10].Value = totVatCancelled > 0 ? -totVatCancelled : (object)string.Empty;
        if (totVatCancelled > 0)
            ws.Cells[row, 10].Style.Numberformat.Format = fmt;

        FinalizeSheet(ws, headers.Length, row + 1);
        return await package.GetAsByteArrayAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Shared helpers
    // ─────────────────────────────────────────────────────────────────
    private static void WriteHeader(ExcelWorksheet ws, string[] headers)
    {
        for (int c = 1; c <= headers.Length; c++)
        {
            ws.Cells[1, c].Value = headers[c - 1];
        }
        using var range = ws.Cells[1, 1, 1, headers.Length];
        range.Style.Font.Bold = true;
        range.Style.Font.Color.SetColor(HeaderTextColor);
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(HeaderColor);
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    private static void FinalizeSheet(ExcelWorksheet ws, int colCount, int lastRow)
    {
        if (lastRow > 2)
        {
            var dataRange = ws.Cells[1, 1, lastRow - 1, colCount];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            var table = ws.Tables.Add(ws.Cells[1, 1, lastRow - 1, colCount], $"Table_{ws.Name.Replace(" ", "_")}");
            table.ShowHeader = true;
            table.TableStyle = TableStyles.Medium2;
        }
        ws.Cells.AutoFitColumns();
    }

    private static void ApplyStripe(ExcelWorksheet ws, int row, int colCount)
    {
        using var range = ws.Cells[row, 1, row, colCount];
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(StripeColor);
    }

    private static void WriteSummaryRow(ExcelWorksheet ws, int row, string label, string value)
    {
        ws.Cells[row, 1].Value = label;
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 2].Value = value;
    }

    private static string GetNumberFormat(CultureInfo culture)
    {
        var dec = culture.NumberFormat.NumberDecimalSeparator;
        var grp = culture.NumberFormat.NumberGroupSeparator;
        return $"#{grp}##0{dec}00";
    }

    private string TranslateInvoiceStatus(string status) => status switch
    {
        "Stamped" => _localizer["Stamped"].Value,
        "Cancelled" => _localizer["Cancelled"].Value,
        "Draft" => _localizer["Draft"].Value,
        "StampError" => _localizer["Stamp Error"].Value,
        _ => status
    };

    private string TranslateGlobalStatus(GlobalInvoiceStatus status) => status switch
    {
        GlobalInvoiceStatus.Stamped => _localizer["Stamped"].Value,
        GlobalInvoiceStatus.Cancelled => _localizer["Cancelled"].Value,
        GlobalInvoiceStatus.Draft => _localizer["Draft"].Value,
        GlobalInvoiceStatus.StampError => _localizer["Stamp Error"].Value,
        _ => status.ToString()
    };

    // ─────────────────────────────────────────────────────────────────
    //  Report 5 — Sales Book (Individual + Global combined)
    // ─────────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportSalesBookAsync(
        InvoiceReportRequestDto request,
        CultureInfo culture,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = request.EndDate?.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);

        var individualInvoices = await context.MexicoInvoices
            .Where(i => (i.StampDate >= startDate && i.StampDate < endDate) ||
                        (i.StampDate == null && i.RequestedInvoiceDate >= startDate && i.RequestedInvoiceDate < endDate))
            .AsNoTracking()
            .ToListAsync(ct);

        var globalInvoices = await context.GlobalInvoices
            .Where(g => (g.StampDate >= startDate && g.StampDate < endDate) ||
                        (g.StampDate == null && g.CreatedAt >= startDate && g.CreatedAt < endDate))
            .AsNoTracking()
            .ToListAsync(ct);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var fmt = GetNumberFormat(culture);

        var ws = package.Workbook.Worksheets.Add(_localizer["Sales Book"].Value);
        var headers = new[]
        {
            _localizer["Date"].Value,
            _localizer["Invoice Type"].Value,
            _localizer["UUID (Folio Fiscal)"].Value,
            _localizer["Serie"].Value,
            _localizer["Folio"].Value,
            _localizer["Sale #"].Value,
            _localizer["Customer RFC"].Value,
            _localizer["Customer Legal Name"].Value,
            _localizer["Subtotal"].Value,
            _localizer["Discount"].Value,
            _localizer["Tax Amount"].Value,
            _localizer["Total"].Value,
            _localizer["Payment Method"].Value,
            _localizer["Payment Form"].Value,
            _localizer["Currency"].Value,
            _localizer["Status"].Value,
            _localizer["Cancellation Date"].Value
        };
        WriteHeader(ws, headers);

        // Build unified list sorted by date
        var individualType = _localizer["Individual"].Value;
        var globalType = _localizer["Global"].Value;

        var rows = new List<(DateTime SortDate, Action<int> Write)>();

        foreach (var inv in individualInvoices)
        {
            var sortDate = inv.StampDate ?? inv.RequestedInvoiceDate ?? inv.CreatedAt;
            rows.Add((sortDate, rowNum =>
            {
                ws.Cells[rowNum, 1].Value = sortDate.ToString("yyyy-MM-dd HH:mm");
                ws.Cells[rowNum, 2].Value = individualType;
                ws.Cells[rowNum, 3].Value = inv.Uuid ?? string.Empty;
                ws.Cells[rowNum, 4].Value = inv.Serie ?? string.Empty;
                ws.Cells[rowNum, 5].Value = inv.Folio;
                ws.Cells[rowNum, 6].Value = inv.SaleId;
                ws.Cells[rowNum, 7].Value = inv.CustomerRfc;
                ws.Cells[rowNum, 8].Value = inv.CustomerLegalName;
                ws.Cells[rowNum, 9].Value = inv.Subtotal;
                ws.Cells[rowNum, 9].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 10].Value = 0m;
                ws.Cells[rowNum, 10].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 11].Value = inv.TaxAmount;
                ws.Cells[rowNum, 11].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 12].Value = inv.Total;
                ws.Cells[rowNum, 12].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 13].Value = inv.PaymentMethod;
                ws.Cells[rowNum, 14].Value = inv.PaymentForm;
                ws.Cells[rowNum, 15].Value = inv.Currency;
                ws.Cells[rowNum, 16].Value = TranslateInvoiceStatus(inv.Status);
                ws.Cells[rowNum, 17].Value = inv.CancellationDate.HasValue ? inv.CancellationDate.Value.ToString("yyyy-MM-dd") : string.Empty;
            }));
        }

        foreach (var g in globalInvoices)
        {
            var sortDate = g.StampDate ?? g.CreatedAt;
            rows.Add((sortDate, rowNum =>
            {
                ws.Cells[rowNum, 1].Value = sortDate.ToString("yyyy-MM-dd HH:mm");
                ws.Cells[rowNum, 2].Value = globalType;
                ws.Cells[rowNum, 3].Value = g.Uuid ?? string.Empty;
                ws.Cells[rowNum, 4].Value = g.Serie ?? string.Empty;
                ws.Cells[rowNum, 5].Value = g.Folio;
                ws.Cells[rowNum, 6].Value = string.Empty;
                ws.Cells[rowNum, 7].Value = "XAXX010101000";
                ws.Cells[rowNum, 8].Value = "PUBLICO EN GENERAL";
                ws.Cells[rowNum, 9].Value = g.Subtotal;
                ws.Cells[rowNum, 9].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 10].Value = g.DiscountAmount;
                ws.Cells[rowNum, 10].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 11].Value = g.TaxAmount;
                ws.Cells[rowNum, 11].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 12].Value = g.Total;
                ws.Cells[rowNum, 12].Style.Numberformat.Format = fmt;
                ws.Cells[rowNum, 13].Value = string.Empty;
                ws.Cells[rowNum, 14].Value = g.PaymentForm;
                ws.Cells[rowNum, 15].Value = "MXN";
                ws.Cells[rowNum, 16].Value = TranslateGlobalStatus(g.Status);
                ws.Cells[rowNum, 17].Value = g.CancellationDate.HasValue ? g.CancellationDate.Value.ToString("yyyy-MM-dd") : string.Empty;
            }));
        }

        var sortedRows = rows.OrderBy(r => r.SortDate).ToList();
        int row = 2;
        foreach (var (_, write) in sortedRows)
        {
            write(row);

            // Color rows by status
            var statusCell = ws.Cells[row, 16].Value?.ToString() ?? string.Empty;
            if (statusCell == _localizer["Cancelled"].Value)
            {
                using var cancelRange = ws.Cells[row, 1, row, headers.Length];
                cancelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cancelRange.Style.Fill.BackgroundColor.SetColor(NoCoverageColor);
            }
            else if (statusCell == _localizer["Stamp Error"].Value)
            {
                using var errorRange = ws.Cells[row, 1, row, headers.Length];
                errorRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                errorRange.Style.Fill.BackgroundColor.SetColor(StampErrorColor);
            }
            else if (row % 2 == 0)
            {
                ApplyStripe(ws, row, headers.Length);
            }

            row++;
        }

        // ── Summary rows (3 rows: Stamped / Cancelled / Stamp Error) ──
        decimal stSubtotal = 0, stTax = 0, stTotal = 0;
        decimal caSubtotal = 0, caTax = 0, caTotal = 0;
        decimal erSubtotal = 0, erTax = 0, erTotal = 0;

        foreach (var inv in individualInvoices)
        {
            if (inv.Status == "Stamped")      { stSubtotal += inv.Subtotal; stTax += inv.TaxAmount; stTotal += inv.Total; }
            else if (inv.Status == "Cancelled") { caSubtotal += inv.Subtotal; caTax += inv.TaxAmount; caTotal += inv.Total; }
            else if (inv.Status == "StampError") { erSubtotal += inv.Subtotal; erTax += inv.TaxAmount; erTotal += inv.Total; }
        }
        foreach (var g in globalInvoices)
        {
            if (g.Status == GlobalInvoiceStatus.Stamped)    { stSubtotal += g.Subtotal; stTax += g.TaxAmount; stTotal += g.Total; }
            else if (g.Status == GlobalInvoiceStatus.Cancelled)   { caSubtotal += g.Subtotal; caTax += g.TaxAmount; caTotal += g.Total; }
            else if (g.Status == GlobalInvoiceStatus.StampError)  { erSubtotal += g.Subtotal; erTax += g.TaxAmount; erTotal += g.Total; }
        }

        void WriteSummaryTotal(int r, string label, decimal subtotal, decimal tax, decimal total, Color bgColor)
        {
            using var range = ws.Cells[r, 1, r, headers.Length];
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(bgColor);
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.Font.Bold = true;
            ws.Cells[r, 1].Value = label;
            ws.Cells[r, 9].Value = subtotal;   ws.Cells[r, 9].Style.Numberformat.Format = fmt;
            ws.Cells[r, 11].Value = tax;       ws.Cells[r, 11].Style.Numberformat.Format = fmt;
            ws.Cells[r, 12].Value = total;     ws.Cells[r, 12].Style.Numberformat.Format = fmt;
        }

        WriteSummaryTotal(row,     _localizer["Total (Stamped)"].Value,   stSubtotal, stTax, stTotal, TotalColor);
        WriteSummaryTotal(row + 1, _localizer["Cancelled"].Value,         caSubtotal, caTax, caTotal, CancelledTotalColor);
        WriteSummaryTotal(row + 2, _localizer["Stamp Error"].Value,       erSubtotal, erTax, erTotal, StampErrorTotalColor);

        FinalizeSheet(ws, headers.Length, row + 2);
        return await package.GetAsByteArrayAsync(ct);
    }

    private string TranslatePeriodicity(GlobalInvoicePeriodicity periodicity) => periodicity switch
    {
        GlobalInvoicePeriodicity.Daily => _localizer["Daily"].Value,
        GlobalInvoicePeriodicity.Weekly => _localizer["Weekly"].Value,
        GlobalInvoicePeriodicity.Biweekly => _localizer["Biweekly"].Value,
        GlobalInvoicePeriodicity.Monthly => _localizer["Monthly"].Value,
        _ => periodicity.ToString()
    };
}
