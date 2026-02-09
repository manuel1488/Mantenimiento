using System.Globalization;
using App.Core.DTOs.Inventory;

namespace App.Core.Interfaces;

/// <summary>
/// Service for exporting inventory data to different formats
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports inventory status to Excel format
    /// </summary>
    /// <param name="request">The export request parameters</param>
    /// <param name="culture">The culture to use for formatting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the file content and filename</returns>
    Task<(byte[] Content, string FileName)> ExportInventoryToExcelAsync(
        InventoryExportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports inventory movement history to Excel format
    /// </summary>
    /// <param name="request">The export request parameters</param>
    /// <param name="culture">The culture to use for formatting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the file content and filename</returns>
    Task<(byte[] Content, string FileName)> ExportInventoryHistoryToExcelAsync(
        InventoryHistoryExportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports inventory status to PDF format
    /// </summary>
    /// <param name="request">The export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the file content and filename</returns>
    Task<(byte[] Content, string FileName)> ExportInventoryToPdfAsync(
        InventoryExportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports inventory movement history to PDF format
    /// </summary>
    /// <param name="request">The export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the file content and filename</returns>
    Task<(byte[] Content, string FileName)> ExportInventoryHistoryToPdfAsync(
        InventoryHistoryExportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports inventory alerts to Excel format
    /// </summary>
    /// <param name="request">The export request parameters</param>
    /// <param name="culture">The culture to use for formatting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the file content and filename</returns>
    Task<(byte[] Content, string FileName)> ExportInventoryAlertsToExcelAsync(
        InventoryExportRequestDto request,
        CultureInfo culture,
        CancellationToken cancellationToken = default);
}