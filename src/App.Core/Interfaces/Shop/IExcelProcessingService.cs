using App.Core.Common;

namespace App.Core.Interfaces.Shop;

/// <summary>
/// Service for processing Excel files and converting them to business objects
/// </summary>
public interface IExcelProcessingService
{
    /// <summary>
    /// Processes an Excel file stream and converts it to product bulk load request
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result with converted data and validation errors</returns>
    Task<Result<ExcelProcessingResult>> ProcessProductExcelFileAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Excel file format and structure before processing
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Result<bool> ValidateExcelFileStructureAsync(
        Stream fileStream, 
        CancellationToken cancellationToken = default);
}