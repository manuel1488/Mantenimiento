using System.Globalization;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Reports;
using App.Core.Interfaces;
using App.Core.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Services.Reports;

public class ExportService : IExportService
{
    private readonly IExcelExportService _excelExportService;
    private readonly IPdfService _pdfService;
    private readonly IInventoryQueryService _inventoryQueryService;
    private readonly IInventoryHistoryService _inventoryHistoryService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ExportOptions _exportOptions;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IExcelExportService excelExportService,
        IPdfService pdfService,
        IInventoryQueryService inventoryQueryService,
        IInventoryHistoryService inventoryHistoryService,
        ICompanySettingsService companySettingsService,
        IOptions<ExportOptions> exportOptions,
        ILogger<ExportService> logger)
    {
        _excelExportService = excelExportService;
        _pdfService = pdfService;
        _inventoryQueryService = inventoryQueryService;
        _inventoryHistoryService = inventoryHistoryService;
        _companySettingsService = companySettingsService;
        _exportOptions = exportOptions.Value;
        _logger = logger;
    }

    public async Task<(byte[] Content, string FileName)> ExportInventoryToExcelAsync(
        InventoryExportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                throw new InvalidOperationException(
                    $"Export request exceeds maximum allowed records ({_exportOptions.MaxExportRecords})");
            }

            var (_, items) = await _inventoryQueryService.GetInventoryStatusAsync(
                page: 1,
                pageSize: request.PageSize,
                searchString: request.SearchString,
                warehouseId: request.WarehouseId,
                hasStock: request.HasStock,
                belowMinStock: request.BelowMinStock,
                aboveMaxStock: request.AboveMaxStock,
                cancellationToken: cancellationToken);

            var content = await _excelExportService.ExportInventoryToExcelAsync(
                items,
                culture,
                cancellationToken);

            var fileName = $"inventory_status_{culture.Name}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return (content, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory to Excel");
            throw;
        }
    }

    
    public async Task<(byte[] Content, string FileName)> ExportInventoryHistoryToExcelAsync(
        InventoryHistoryExportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxExportRecords)
            {
                throw new InvalidOperationException(
                    $"Export request exceeds maximum allowed records ({_exportOptions.MaxExportRecords})");
            }

            // Usar el nuevo método paginado que aplica filtros en la base de datos
            var (movements, _) = await _inventoryHistoryService.GetWarehouseMovementHistoryAsync(
                warehouseId: request.WarehouseId,
                startDate: request.StartDate,
                endDate: request.EndDate,
                searchString: request.SearchString,
                movementType: request.MovementType,
                movementSubType: null,
                page: 0,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            var content = await _excelExportService.ExportMovementHistoryToExcelAsync(
                movements.ToList(),
                culture,
                cancellationToken);

            var fileName = $"inventory_movement_history_{culture.Name}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return (content, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting movement history to Excel");
            throw;
        }
    }

    public async Task<(byte[] Content, string FileName)> ExportInventoryToPdfAsync(
        InventoryExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxPdfRecords)
            {
                throw new InvalidOperationException(
                    $"PDF request exceeds maximum allowed records ({_exportOptions.MaxPdfRecords})");
            }

            var (_, items) = await _inventoryQueryService.GetInventoryStatusAsync(
                page: 1,
                pageSize: request.PageSize,
                searchString: request.SearchString,
                warehouseId: request.WarehouseId,
                hasStock: request.HasStock,
                belowMinStock: request.BelowMinStock,
                aboveMaxStock: request.AboveMaxStock,
                cancellationToken: cancellationToken);

            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;
            var reportData = new BaseReportDto<InventoryDto>
            {
                Movements = items,
                TimeZone = timeZone,
                GeneratedAt = DateTime.UtcNow
            };

            var content = await _pdfService.GeneratePdfFromViewAsync(
                "/Views/Reports/Inventory/InventoryReport.cshtml",
                reportData,
                cancellationToken);

            var fileName = $"inventory_report_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (content, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory PDF");
            throw;
        }
    }

    public async Task<(byte[] Content, string FileName)> ExportInventoryHistoryToPdfAsync(
        InventoryHistoryExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.PageSize > _exportOptions.MaxPdfRecords)
            {
                throw new InvalidOperationException(
                    $"PDF request exceeds maximum allowed records ({_exportOptions.MaxPdfRecords})");
            }

            // Usar el nuevo método paginado que aplica filtros en la base de datos
            var (movements, _) = await _inventoryHistoryService.GetWarehouseMovementHistoryAsync(
                warehouseId: request.WarehouseId,
                startDate: request.StartDate,
                endDate: request.EndDate,
                searchString: request.SearchString,
                movementType: request.MovementType,
                movementSubType: null,
                page: 0, // Empezar desde la primera página
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;
            var reportData = new BaseReportDto<InventoryMovementDto>
            {
                Movements = movements.ToList(),
                TimeZone = timeZone,
                GeneratedAt = DateTime.UtcNow
            };

            var content = await _pdfService.GeneratePdfFromViewAsync(
                "/Views/Reports/Inventory/MovementHistoryReport.cshtml",
                reportData,
                cancellationToken);

            var fileName = $"inventory_movement_history_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (content, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating movement history PDF");
            throw;
        }
    }

    public async Task<(byte[] Content, string FileName)> ExportInventoryAlertsToExcelAsync(
        InventoryExportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting inventory alerts to Excel");
            
            // Obtener las alertas
            var alerts = await _inventoryHistoryService.GetCurrentAlertsAsync(
                request.WarehouseId,
                cancellationToken);

            // Aplicar filtros adicionales
            var filteredAlerts = alerts.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(request.SearchString))
            {
                filteredAlerts = filteredAlerts.Where(x =>
                    x.ProductName.Contains(request.SearchString, StringComparison.OrdinalIgnoreCase) ||
                    x.ProductCode.Contains(request.SearchString, StringComparison.OrdinalIgnoreCase) ||
                    x.WarehouseName.Contains(request.SearchString, StringComparison.OrdinalIgnoreCase));
            }
            
            // Si se pasa un tipo de alerta específico, filtrar por ese tipo
            if (!string.IsNullOrWhiteSpace(request.MovementType))
            {
                filteredAlerts = filteredAlerts.Where(x => x.AlertType == request.MovementType);
            }
            
            // Limitar la cantidad según pageSize
            var limitedAlerts = filteredAlerts
                .Take(request.PageSize)
                .ToList();
            
            // Generar el archivo Excel
            var excelBytes = await _excelExportService.ExportAlertsToExcelAsync(
                limitedAlerts,
                culture,
                cancellationToken);
            
            // Nombre del archivo
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Inventory_Alerts_{timestamp}.xlsx";
            
            return (excelBytes, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory alerts to Excel");
            throw;
        }
    }
}