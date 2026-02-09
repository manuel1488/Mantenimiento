using System.Globalization;

using App.Core.DTOs.Reports;
using App.Core.Interfaces;
using App.Core.Options;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace App.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SalesReportController : ControllerBase
{
    private readonly ISalesReportService _reportService;
    private readonly IStringLocalizer<SalesReportController> _localizer;
    private readonly ExportOptions _exportOptions;
    private readonly ILogger<SalesReportController> _logger;

    public SalesReportController(
        ISalesReportService reportService,
        IStringLocalizer<SalesReportController> localizer,
        IOptions<ExportOptions> exportOptions,
        ILogger<SalesReportController> logger)
    {
        _reportService = reportService;
        _localizer = localizer;
        _exportOptions = exportOptions.Value;
        _logger = logger;
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportToExcel([FromQuery] SalesReportRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize <= 0)
            {
                return BadRequest(_localizer["PageSize must be greater than 0"]);
            }

            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                return BadRequest(_localizer["Export request exceeds maximum allowed records ({0})",
                    _exportOptions.MaxExportRecords]);
            }

            var culture = CultureInfo.CurrentCulture;
            var content = await _reportService.ExportSalesReportToExcelAsync(
                request, culture, cancellationToken);

            var startDate = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var endDate = request.EndDate?.ToString("yyyyMMdd") ?? "today";
            var fileName = $"sales_report_{startDate}_to_{endDate}.xlsx";

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report to Excel");
            return StatusCode(500, _localizer["Error exporting sales report"]);
        }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportToPdf([FromQuery] SalesReportRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize <= 0)
            {
                return BadRequest(_localizer["PageSize must be greater than 0"]);
            }

            if (request.PageSize > _exportOptions.MaxPdfRecords)
            {
                return BadRequest(_localizer["Print request exceeds maximum allowed records ({0})",
                    _exportOptions.MaxPdfRecords]);
            }

            var content = await _reportService.ExportSalesReportToPdfAsync(
                request, cancellationToken);

            var startDate = request.StartDate?.ToString("yyyyMMdd") ?? "all";
            var endDate = request.EndDate?.ToString("yyyyMMdd") ?? "today";
            var fileName = $"sales_report_{startDate}_to_{endDate}.pdf";

            return File(content, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report to PDF");
            return StatusCode(500, _localizer["Error exporting sales report"]);
        }
    }
}
