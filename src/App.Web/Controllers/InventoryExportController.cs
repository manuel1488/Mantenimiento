using System.Globalization;

using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Core.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace App.Web.Controllers;

/// <summary>
/// Controller for handling inventory exports and reports
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InventoryExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IStringLocalizer<InventoryExportController> L;
    private readonly ExportOptions _exportOptions;
    private readonly ILogger<InventoryExportController> _logger;

    public InventoryExportController(
        IExportService exportService,
        IStringLocalizer<InventoryExportController> localizer,
        IOptions<ExportOptions> exportOptions,
        ILogger<InventoryExportController> logger)
    {
        _exportService = exportService;
        L = localizer;
        _exportOptions = exportOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Exports current inventory status to Excel
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] InventoryExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize <= 0)
            {
                return BadRequest(L["PageSize must be greater than 0"]);
            }

            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                return BadRequest(L["Export request exceeds maximum allowed records ({0})", 
                    _exportOptions.MaxExportRecords]);
            }

            var culture = CultureInfo.CurrentCulture;
            var (content, fileName) = await _exportService.ExportInventoryToExcelAsync(
                request, culture, cancellationToken);

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory to Excel");
            return StatusCode(500, L["Error exporting inventory"]);
        }
    }

    /// <summary>
    /// Exports inventory movement history to Excel
    /// </summary>
    [HttpGet("exportHistory")]
    public async Task<IActionResult> ExportHistoryToExcel(
        [FromQuery] InventoryHistoryExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize <= 0)
            {
                return BadRequest(L["PageSize must be greater than 0"]);
            }

            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                return BadRequest(L["Export request exceeds maximum allowed records ({0})", 
                    _exportOptions.MaxExportRecords]);
            }

            var culture = CultureInfo.CurrentCulture;
            var (content, fileName) = await _exportService.ExportInventoryHistoryToExcelAsync(
                request, culture, cancellationToken);

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting movement history to Excel");
            return StatusCode(500, L["Error exporting movement history"]);
        }
    }

    /// <summary>
    /// Exports current inventory status to PDF
    /// </summary>
    [HttpGet("print")]
    public async Task<IActionResult> PrintInventory(
        [FromQuery] InventoryExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize <= 0)
            {
                return BadRequest(L["PageSize must be greater than 0"]);
            }

            if (request.PageSize > _exportOptions.MaxPdfRecords)
            {
                return BadRequest(L["Print request exceeds maximum allowed records ({0})", 
                    _exportOptions.MaxPdfRecords]);
            }

            var (content, fileName) = await _exportService.ExportInventoryToPdfAsync(
                request, cancellationToken);

            return File(content, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing inventory");
            return StatusCode(500, L["Error printing inventory"]);
        }
    }

    /// <summary>
    /// Exports inventory movement history to PDF
    /// </summary>
    [HttpGet("printHistory")]
    public async Task<IActionResult> PrintMovementHistory(
        [FromQuery] InventoryHistoryExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize <= 0)
            {
                return BadRequest(L["PageSize must be greater than 0"]);
            }

            if (request.PageSize > _exportOptions.MaxPdfRecords)
            {
                return BadRequest(L["Print request exceeds maximum allowed records ({0})", 
                    _exportOptions.MaxPdfRecords]);
            }

            var (content, fileName) = await _exportService.ExportInventoryHistoryToPdfAsync(
                request, cancellationToken);

            return File(content, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing movement history");
            return StatusCode(500, L["Error printing movement history"]);
        }
    }


    /// <summary>
    /// Exports transfer history to Excel
    /// </summary>
    [HttpGet("exportTransfers")]
    public async Task<IActionResult> ExportTransfersToExcel(
        [FromQuery] int? sourceLocationId,
        [FromQuery] int? destinationLocationId, 
        [FromQuery] string? searchString,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            if (pageSize <= 0)
            {
                return BadRequest(L["PageSize must be greater than 0"]);
            }

            if (pageSize > _exportOptions.MaxExportRecords)
            {
                return BadRequest(L["Export request exceeds maximum allowed records ({0})", 
                    _exportOptions.MaxExportRecords]);
            }

            var request = new InventoryHistoryExportRequestDto
            {
                SearchString = searchString,
                LocationId = sourceLocationId,
                StartDate = startDate,
                EndDate = endDate,
                PageSize = pageSize,
                MovementType = InventoryMovementType.Transfer
            };

            var culture = CultureInfo.CurrentCulture;
            var (content, fileName) = await _exportService.ExportInventoryHistoryToExcelAsync(
                request, culture, cancellationToken);

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transfers to Excel");
            return StatusCode(500, L["Error exporting transfers"]);
        }
    }

    /// <summary>
    /// Exports stock inputs (StockIn, Purchase, Return) to Excel
    /// </summary>
    [HttpGet("exportInputs")]
    public async Task<IActionResult> ExportInputsToExcel(
        [FromQuery] int? locationId,
        [FromQuery] string? movementType,
        [FromQuery] string? searchString,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            if (pageSize <= 0)
                return BadRequest(L["PageSize must be greater than 0"]);

            if (pageSize > _exportOptions.MaxExportRecords)
                return BadRequest(L["Export request exceeds maximum allowed records ({0})", _exportOptions.MaxExportRecords]);

            string[] movementTypes = string.IsNullOrWhiteSpace(movementType)
                ? [InventoryMovementType.StockIn, InventoryMovementType.Purchase, InventoryMovementType.Return]
                : [movementType];

            var request = new InventoryHistoryExportRequestDto
            {
                SearchString = searchString,
                LocationId = locationId,
                MovementTypes = movementTypes,
                StartDate = startDate,
                EndDate = endDate,
                PageSize = pageSize
            };

            var culture = CultureInfo.CurrentCulture;
            var (content, fileName) = await _exportService.ExportInventoryHistoryToExcelAsync(
                request, culture, cancellationToken);

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting stock inputs to Excel");
            return StatusCode(500, L["Error exporting stock inputs"]);
        }
    }

    /// <summary>
    /// Exports inventory adjustments to Excel
    /// </summary>
    [HttpGet("exportAdjustments")]
    public async Task<IActionResult> ExportAdjustmentsToExcel(
        [FromQuery] int? locationId,
        [FromQuery] string? movementSubType,
        [FromQuery] string? searchString,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            if (pageSize <= 0)
                return BadRequest(L["PageSize must be greater than 0"]);

            if (pageSize > _exportOptions.MaxExportRecords)
                return BadRequest(L["Export request exceeds maximum allowed records ({0})", _exportOptions.MaxExportRecords]);

            var request = new InventoryHistoryExportRequestDto
            {
                SearchString = searchString,
                LocationId = locationId,
                MovementType = InventoryMovementType.Adjustment,
                MovementSubType = movementSubType,
                StartDate = startDate,
                EndDate = endDate,
                PageSize = pageSize
            };

            var culture = CultureInfo.CurrentCulture;
            var (content, fileName) = await _exportService.ExportInventoryHistoryToExcelAsync(
                request, culture, cancellationToken);

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting adjustments to Excel");
            return StatusCode(500, L["Error exporting adjustments"]);
        }
    }

    /// <summary>
    /// Exports inventory alerts to Excel
    /// </summary>
    [HttpGet("exportAlerts")]
    public async Task<IActionResult> ExportAlertsToExcel(
        [FromQuery] int? locationId,
        [FromQuery] string? alertType,
        [FromQuery] string? searchString,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            if (pageSize <= 0)
            {
                return BadRequest(L["PageSize must be greater than 0"]);
            }

            if (pageSize > _exportOptions.MaxExportRecords)
            {
                return BadRequest(L["Export request exceeds maximum allowed records ({0})", 
                    _exportOptions.MaxExportRecords]);
            }

            var request = new InventoryExportRequestDto
            {
                SearchString = searchString,
                LocationId = locationId,
                PageSize = pageSize,
                // Usamos un campo existente para pasar alertType
                MovementType = alertType
            };

            var culture = CultureInfo.CurrentCulture;
            var (content, fileName) = await _exportService.ExportInventoryAlertsToExcelAsync(
                request, culture, cancellationToken);

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting alerts to Excel");
            return StatusCode(500, L["Error exporting alerts"]);
        }
    }
}