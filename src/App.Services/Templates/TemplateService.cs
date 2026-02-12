using System.Drawing;
using System.Text;

using App.Core.Constants;
using App.Core.DTOs.UnitMeasure;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;

namespace App.Services.Templates;

public class TemplateService : ITemplateService
{
    private readonly IStringLocalizer<TemplateService> _localizer;
    private readonly ILogger<TemplateService> _logger;
    private readonly IUnitMeasureService _unitMeasureService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IInventoryColumnMappingService _columnMappingService;
    private readonly IWholesaleTierService _wholesaleTierService;

    public TemplateService(IStringLocalizer<TemplateService> localizer,
        ILogger<TemplateService> logger,
        IUnitMeasureService unitMeasureService,
        ICompanySettingsService companySettingsService,
        IInventoryColumnMappingService columnMappingService,
        IWholesaleTierService wholesaleTierService)
    {
        _localizer = localizer;
        _logger = logger;
        _unitMeasureService = unitMeasureService;
        _companySettingsService = companySettingsService;
        _columnMappingService = columnMappingService;
        _wholesaleTierService = wholesaleTierService;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<byte[]> GenerateInventoryTemplateAsync()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Inventory");

         var columnMapping = _columnMappingService.GetColumnMappingForCurrentCulture();
        var headers = new[] { "ProductCode", "Quantity", "MinStock", "MaxStock" };

        // Define headers using localizer
        for (int i = 0; i < headers.Length; i++)
        {
            var translatedHeader = columnMapping[headers[i]];
            worksheet.Cells[1, i + 1].Value = translatedHeader;
        }

        // Add example data
        worksheet.Cells[2, 1].Value = "PROD001";
        worksheet.Cells[2, 2].Value = 100;
        worksheet.Cells[2, 3].Value = 10;
        worksheet.Cells[2, 4].Value = 1000;

        // Format headers
        using (var range = worksheet.Cells[1, 1, 1, 4])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        // Validations
        // Quantity must be greater than or equal to 0
        var quantityValidation = worksheet.Cells[2, 2, 1000, 2].DataValidation.AddDecimalDataValidation();
        quantityValidation.Operator = ExcelDataValidationOperator.greaterThanOrEqual;
        quantityValidation.Formula.Value = 0;
        quantityValidation.ErrorTitle = _localizer["Invalid Quantity"];
        quantityValidation.Error = _localizer["Quantity must be greater than or equal to 0"];
        quantityValidation.ShowErrorMessage = true;
        quantityValidation.AllowBlank = false;

        // MinStock must be greater than or equal to 0
        var minStockValidation = worksheet.Cells[2, 3, 1000, 3].DataValidation.AddDecimalDataValidation();
        minStockValidation.Operator = ExcelDataValidationOperator.greaterThanOrEqual;
        minStockValidation.Formula.Value = 0;
        minStockValidation.ErrorTitle = _localizer["Invalid Min Stock"];
        minStockValidation.Error = _localizer["Min Stock must be greater than or equal to 0"];
        minStockValidation.ShowErrorMessage = true;
        minStockValidation.AllowBlank = true;

        // MaxStock must be greater than or equal to 0
        var maxStockValidation = worksheet.Cells[2, 4, 1000, 4].DataValidation.AddDecimalDataValidation();
        maxStockValidation.Operator = ExcelDataValidationOperator.greaterThanOrEqual;
        maxStockValidation.Formula.Value = 0;
        maxStockValidation.ErrorTitle = _localizer["Invalid Max Stock"];
        maxStockValidation.Error = _localizer["Max Stock must be greater than or equal to 0"];
        maxStockValidation.ShowErrorMessage = true;
        maxStockValidation.AllowBlank = true;

        // Format for description
        using (var range = worksheet.Cells[1, 1, 1, 4])
        {
            range.Style.Font.Italic = true;
            range.Style.Font.Color.SetColor(Color.DarkGray);
            range.Style.WrapText = true;
            worksheet.Row(1).Height = 30; // Height for wrapped text
        }

        return await package.GetAsByteArrayAsync();
    }

    public async Task<byte[]> GenerateProductTemplateAsync()
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Get company settings to determine country
            var companySettings = await _companySettingsService.GetSettingsAsync();
            var countryCode = companySettings?.CountryCode ?? "MX"; // Default to Mexico if not found

            // Get unit measures for the configured country
            var unitMeasures = await _unitMeasureService.GetActiveUnitMeasuresAsync(countryCode);

            // Define headers with localized text
            var headers = GetProductoHeadersName();

            // Set headers
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Add example row
            var row = 2;
            worksheet.Cells[row, 1].Value = ""; // Code - optional, will auto-generate
            worksheet.Cells[row, 2].Value = _localizer["Sample Product"];
            worksheet.Cells[row, 3].Value = _localizer["Sample Brand"];
            worksheet.Cells[row, 4].Value = _localizer["Product description"];
            worksheet.Columns[5].Style.Numberformat.Format = "@";
            worksheet.Cells[row, 5].Value = "123456789012";
            worksheet.Cells[row, 6].Value = 1.0;

            // Use first available unit measure as example, or default value
            var exampleUnitCode = unitMeasures.FirstOrDefault()?.Code ?? "PZA";
            worksheet.Cells[row, 7].Value = exampleUnitCode;

            worksheet.Cells[row, 8].Value = 100.00;
            worksheet.Cells[row, 9].Value = "true";
            worksheet.Cells[row, 10].Value = "true";
            worksheet.Columns[11].Style.Numberformat.Format = "@";
            worksheet.Cells[row, 11].Value = "01010101"; // Example SAT code
            worksheet.Cells[row, 12].Value = "false";
            worksheet.Cells[row, 13].Value = "false";

            // Add wholesale tier columns dynamically
            var tiersResult = await _wholesaleTierService.GetActiveTiersAsync();
            var activeTiers = tiersResult.IsSuccess ? tiersResult.Value?.ToList() ?? new() : new();
            var baseColumnCount = headers.Length;

            for (int t = 0; t < activeTiers.Count; t++)
            {
                var tier = activeTiers[t];
                var minQtyCol = baseColumnCount + (t * 2) + 1; // 1-indexed
                var discountCol = minQtyCol + 1;

                // Set headers with green background
                var minQtyHeaderCell = worksheet.Cells[1, minQtyCol];
                minQtyHeaderCell.Value = $"{_localizer["Min Qty"]} {tier.Name}";
                minQtyHeaderCell.Style.Font.Bold = true;
                minQtyHeaderCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                minQtyHeaderCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(39, 174, 96));
                minQtyHeaderCell.Style.Font.Color.SetColor(Color.White);

                var discountHeaderCell = worksheet.Cells[1, discountCol];
                discountHeaderCell.Value = $"{_localizer["Discount %"]} {tier.Name}";
                discountHeaderCell.Style.Font.Bold = true;
                discountHeaderCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                discountHeaderCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(39, 174, 96));
                discountHeaderCell.Style.Font.Color.SetColor(Color.White);

                // Set example values
                worksheet.Cells[row, minQtyCol].Value = (t + 1) * 10; // 10, 20, etc.
                worksheet.Cells[row, discountCol].Value = (t + 1) * 5; // 5%, 10%, etc.

                // Add comments
                worksheet.Cells[1, minQtyCol].AddComment(
                    _localizer["Minimum quantity to qualify for {0} pricing. Leave empty or 0 to skip.", tier.Name], "System");
                worksheet.Cells[1, discountCol].AddComment(
                    _localizer["Discount percentage for {0} tier (0-100). Leave empty or 0 to skip.", tier.Name], "System");
            }

            var totalColumnCount = baseColumnCount + (activeTiers.Count * 2);

            // Add comments/notes for guidance using localized text
            worksheet.Cells[1, 1].AddComment(_localizer["Optional. Leave empty to auto-generate product code."], "System");

            // Create dynamic comment for unit codes based on database data
            var unitCodesComment = GenerateUnitCodesComment(unitMeasures);
            var unitComment = worksheet.Cells[1, 7].AddComment(unitCodesComment, "System");

            // Customize comment size and appearance
            unitComment.AutoFit = true; // Disable auto-fit to set custom size

            worksheet.Cells[1, 9].AddComment(_localizer["Boolean: true/false, 1/0, yes/no, y/n"], "System");
            worksheet.Cells[1, 10].AddComment(_localizer["Boolean: true/false, 1/0, yes/no, y/n"], "System");
            worksheet.Cells[1, 11].AddComment(_localizer["Required for Mexico. Use valid SAT product/service code."], "System");
            worksheet.Cells[1, 12].AddComment(_localizer["Boolean: true/false, 1/0, yes/no, y/n. Indicates if product can be sold in partial quantities."], "System");
            worksheet.Cells[1, 13].AddComment(_localizer["Boolean: true/false, 1/0, yes/no, y/n. Indicates if product price can be customized during sales (useful for liquid products)."], "System");

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Set minimum column width for all columns including wholesale
            for (int i = 1; i <= totalColumnCount; i++)
            {
                if (worksheet.Column(i).Width < 15)
                    worksheet.Column(i).Width = 15;
            }

            return await Task.FromResult(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating product template");
            throw;
        }
    }

    /// <summary>
    /// Generates a comment string with all available unit measure codes for the configured country
    /// </summary>
    /// <param name="unitMeasures">List of unit measures from database</param>
    /// <returns>Formatted comment string with unit codes and descriptions</returns>
    private string GenerateUnitCodesComment(IList<UnitMeasureDto> unitMeasures)
    {
        if (unitMeasures == null || !unitMeasures.Any())
        {
            return _localizer["No unit measures found for this country"];
        }

        var commentBuilder = new StringBuilder();
        commentBuilder.AppendLine(_localizer["Available unit codes:"]);

        foreach (var unit in unitMeasures.Take(20)) // Limit to first 10 to avoid very long comments
        {
            commentBuilder.AppendLine($"{unit.Code}: {unit.Name}");
        }

        if (unitMeasures.Count > 20)
        {
            commentBuilder.AppendLine(_localizer["... and {0} more", unitMeasures.Count - 20]);
        }

        return commentBuilder.ToString().TrimEnd();
    }

    public string[] GetProductoHeadersName()
    {
        return ProductTemplateColumns.GetProductColumnConfigurations(_localizer)
            .Select(c => c.GetLocalizedName())
            .ToArray();
    }
}