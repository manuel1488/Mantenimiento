using System.Globalization;

using App.Core.Constants;
using App.Core.DTOs.Billing;
using App.Core.Interfaces.Billing;
using App.Core.Options;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InvoiceReportController : ControllerBase
{
    private readonly IInvoiceReportService _reportService;
    private readonly IStringLocalizer<InvoiceReportController> _localizer;
    private readonly ExportOptions _exportOptions;
    private readonly ILogger<InvoiceReportController> _logger;

    public InvoiceReportController(
        IInvoiceReportService reportService,
        IStringLocalizer<InvoiceReportController> localizer,
        IOptions<ExportOptions> exportOptions,
        ILogger<InvoiceReportController> logger)
    {
        _reportService = reportService;
        _localizer = localizer;
        _exportOptions = exportOptions.Value;
        _logger = logger;
    }

    [HttpGet("individual")]
    [Authorize(Policy = ApplicationClaims.Admin.ExportBillingReports)]
    public async Task<IActionResult> ExportIndividual([FromQuery] InvoiceReportRequestDto request, CancellationToken ct)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
                request.PageSize = _exportOptions.MaxExportRecords;

            var culture = CultureInfo.CurrentCulture;
            var content = await _reportService.ExportIndividualInvoicesAsync(request, culture, ct);
            var start = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var end = request.EndDate?.ToString("yyyyMMdd") ?? "today";

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"individual_invoices_{start}_{end}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting individual invoices");
            return StatusCode(500, _localizer["Error exporting report"]);
        }
    }

    [HttpGet("global")]
    [Authorize(Policy = ApplicationClaims.Admin.ExportBillingReports)]
    public async Task<IActionResult> ExportGlobal([FromQuery] InvoiceReportRequestDto request, CancellationToken ct)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
                request.PageSize = _exportOptions.MaxExportRecords;

            var culture = CultureInfo.CurrentCulture;
            var content = await _reportService.ExportGlobalInvoicesAsync(request, culture, ct);
            var start = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var end = request.EndDate?.ToString("yyyyMMdd") ?? "today";

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"global_invoices_{start}_{end}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting global invoices");
            return StatusCode(500, _localizer["Error exporting report"]);
        }
    }

    [HttpGet("conciliation")]
    [Authorize(Policy = ApplicationClaims.Admin.ExportBillingReports)]
    public async Task<IActionResult> ExportConciliation([FromQuery] InvoiceReportRequestDto request, CancellationToken ct)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
                request.PageSize = _exportOptions.MaxExportRecords;

            var culture = CultureInfo.CurrentCulture;
            var content = await _reportService.ExportConciliationAsync(request, culture, ct);
            var start = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var end = request.EndDate?.ToString("yyyyMMdd") ?? "today";

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"conciliation_{start}_{end}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting conciliation");
            return StatusCode(500, _localizer["Error exporting report"]);
        }
    }

    [HttpGet("vat")]
    [Authorize(Policy = ApplicationClaims.Admin.ExportBillingReports)]
    public async Task<IActionResult> ExportVat([FromQuery] InvoiceReportRequestDto request, CancellationToken ct)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
                request.PageSize = _exportOptions.MaxExportRecords;

            var culture = CultureInfo.CurrentCulture;
            var content = await _reportService.ExportVatReportAsync(request, culture, ct);
            var start = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var end = request.EndDate?.ToString("yyyyMMdd") ?? "today";

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"vat_report_{start}_{end}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting VAT report");
            return StatusCode(500, _localizer["Error exporting report"]);
        }
    }

    [HttpGet("sales-book")]
    [Authorize(Policy = ApplicationClaims.Admin.ExportBillingReports)]
    public async Task<IActionResult> ExportSalesBook([FromQuery] InvoiceReportRequestDto request, CancellationToken ct)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
                request.PageSize = _exportOptions.MaxExportRecords;

            var culture = CultureInfo.CurrentCulture;
            var content = await _reportService.ExportSalesBookAsync(request, culture, ct);
            var start = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var end = request.EndDate?.ToString("yyyyMMdd") ?? "today";

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"sales_book_{start}_{end}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales book");
            return StatusCode(500, _localizer["Error exporting report"]);
        }
    }
}
