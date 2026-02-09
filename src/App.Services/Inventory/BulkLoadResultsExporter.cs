using System.Drawing;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace App.Services.inventory;

public class BulkLoadResultsExporter : IBulkLoadResultsExporter
{
    private readonly IStringLocalizer<BulkLoadResultsExporter> _localizer;
    private readonly ILogger<BulkLoadResultsExporter> _logger;

    public BulkLoadResultsExporter(
        IStringLocalizer<BulkLoadResultsExporter> localizer,
        ILogger<BulkLoadResultsExporter> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<byte[]> ExportAsync(
        List<BulkInventoryLoadResultDto> results, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var worksheet = package.Workbook.Worksheets.Add(_localizer["Import Results"]);

            // Add headers
            worksheet.Cells[1, 1].Value = _localizer["Code"];
            worksheet.Cells[1, 2].Value = _localizer["Name"];
            worksheet.Cells[1, 3].Value = _localizer["Quantity"];
            worksheet.Cells[1, 4].Value = _localizer["Min Stock"];
            worksheet.Cells[1, 5].Value = _localizer["Max Stock"];
            worksheet.Cells[1, 6].Value = _localizer["Status"];
            worksheet.Cells[1, 7].Value = _localizer["Error"];

            // Add data
            var row = 2;
            foreach (var result in results)
            {
                worksheet.Cells[row, 1].Value = result.ProductCode;
                worksheet.Cells[row, 2].Value = result.ProductName;
                worksheet.Cells[row, 3].Value = result.Quantity;
                worksheet.Cells[row, 4].Value = result.MinStock;
                worksheet.Cells[row, 5].Value = result.MaxStock;
                worksheet.Cells[row, 6].Value = result.Success ? _localizer["Success"] : _localizer["Failed"];
                worksheet.Cells[row, 7].Value = result.Error;

                // Format numeric columns
                worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";

                // Color status cell based on result
                var statusCell = worksheet.Cells[row, 6];
                if (result.Success)
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 239, 206)); // Light green
                }
                else
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 199, 206)); // Light red
                }

                row++;
            }

            // Add summary section
            row += 2;
            worksheet.Cells[row, 1].Value = _localizer["Summary"];
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = _localizer["Total Records"];
            worksheet.Cells[row, 2].Value = results.Count;

            row++;
            worksheet.Cells[row, 1].Value = _localizer["Successful"];
            worksheet.Cells[row, 2].Value = results.Count(x => x.Success);

            row++;
            worksheet.Cells[row, 1].Value = _localizer["Failed"];
            worksheet.Cells[row, 2].Value = results.Count(x => !x.Success);

            // Format headers
            var headerRange = worksheet.Cells[1, 1, 1, 7];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 237, 237));
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Add borders
            var dataRange = worksheet.Cells[1, 1, row, 7];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            return await package.GetAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting bulk load results");
            throw;
        }
    }
}