using System.Globalization;
using System.Text;
using OfficeOpenXml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Product;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;

namespace App.Services.Products;

/// <summary>
/// Service responsible for processing Excel files and converting them to business objects.
/// This service acts like a skilled translator, converting spreadsheet data into our domain language.
/// </summary>
public class ExcelProcessingService : IExcelProcessingService
{
    private readonly IStringLocalizer<ExcelProcessingService> _localizer;
    private readonly ILogger<ExcelProcessingService> _logger;
    private readonly IWholesaleSettingsService _wholesaleSettingsService;

    public ExcelProcessingService(
        IStringLocalizer<ExcelProcessingService> localizer,
        ILogger<ExcelProcessingService> logger,
        IWholesaleSettingsService wholesaleSettingsService)
    {
        _localizer = localizer;
        _logger = logger;
        _wholesaleSettingsService = wholesaleSettingsService;
    }

    public async Task<Result<ExcelProcessingResult>> ProcessProductExcelFileAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage(fileStream);

            // Validate basic file structure
            var validationResult = ValidateBasicStructure(package);
            if (!validationResult.IsSuccess)
            {
                return Result<ExcelProcessingResult>.Failure(validationResult.Error!);
            }

            var settingsResult = await _wholesaleSettingsService.GetSettingsAsync(cancellationToken);
            var wholesaleMode = settingsResult.IsSuccess && settingsResult.Value != null
                ? settingsResult.Value.PriceMode
                : WholesalePriceMode.Percentage;

            var worksheet = package.Workbook.Worksheets[0];
            var result = ProcessWorksheet(worksheet, wholesaleMode, cancellationToken);

            return Result<ExcelProcessingResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Excel file");
            return Result<ExcelProcessingResult>.Failure(
                _localizer["Unexpected error processing Excel file: {0}", ex.Message]);
        }
    }

    public Result<bool> ValidateExcelFileStructureAsync(
        Stream fileStream, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage(fileStream);
            var validationResult = ValidateBasicStructure(package);
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Excel file structure");
            return Result<bool>.Failure(
                _localizer["Error validating file structure: {0}", ex.Message]);
        }
    }

    /// <summary>
    /// Validates the basic structure of the Excel package - like checking if a building has walls before examining the rooms
    /// </summary>
    private Result<bool> ValidateBasicStructure(ExcelPackage package)
    {
        // Check if file has worksheets
        if (package.Workbook.Worksheets.Count == 0)
        {
            return Result<bool>.Failure(_localizer["Excel file contains no worksheets"]);
        }

        var worksheet = package.Workbook.Worksheets[0];
        
        // Check if worksheet has data
        if (worksheet.Dimension == null)
        {
            return Result<bool>.Failure(_localizer["Excel worksheet is empty"]);
        }

        // Validate headers exist and match expected structure
        var headerValidation = ValidateHeaders(worksheet);
        if (!headerValidation.IsSuccess)
        {
            return headerValidation;
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Validates that the Excel headers match our expected product template structure
    /// </summary>
    private Result<bool> ValidateHeaders(ExcelWorksheet worksheet)
    {
        var columnMapping = BuildColumnMapping(worksheet);
        var expectedHeaders = ProductTemplateColumns.GetProductColumnConfigurations(_localizer)
            .Select(h => NormalizeColumnHeader(h.GetLocalizedName()))
            .ToList();

        var missingColumns = expectedHeaders
            .Where(header => !columnMapping.ContainsKey(header))
            .ToList();
        if (missingColumns.Any())
        {
            var originalHeaders = ProductTemplateColumns.GetProductColumnConfigurations(_localizer)
                .Select(h => h.GetLocalizedName())
                .ToList();

            var missingOriginalHeaders = missingColumns
                .Select(normalized => originalHeaders.FirstOrDefault(original => 
                    NormalizeColumnHeader(original) == normalized) ?? normalized)
                .ToList();

            return Result<bool>.Failure(
                _localizer["Missing required columns: {0}", string.Join(", ", missingOriginalHeaders)]);
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Processes the worksheet data and converts it to our domain objects
    /// </summary>
    private ExcelProcessingResult ProcessWorksheet(
        ExcelWorksheet worksheet,
        WholesalePriceMode wholesaleMode,
        CancellationToken cancellationToken)
    {
        var result = new ExcelProcessingResult
        {
            Request = new BulkProductLoadRequestDto
            {
                Items = new List<ProductBulkLoadDto>()
            },
            SheetName = worksheet.Name,
            TotalRows = worksheet.Dimension?.Rows ?? 0
        };

        var columnMapping = BuildColumnMapping(worksheet);

        // Detect wholesale tier columns (dynamic columns beyond the standard ones)
        var wholesaleTierColumns = DetectWholesaleTierColumns(worksheet);

        // Override IsFixedPrice if global mode is FixedPrice — allows old templates with "Discount %" header to work
        if (wholesaleMode == WholesalePriceMode.FixedPrice)
        {
            wholesaleTierColumns = wholesaleTierColumns.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.MinQtyCol, kvp.Value.ValueCol, IsFixedPrice: true));
        }

        // Process data rows (starting from row 2, as row 1 contains headers)
        if (worksheet.Dimension != null)
        {
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                try
                {
                    var product = ProcessDataRow(worksheet, row, columnMapping, result.Errors);
                    if (product != null)
                    {
                        // Process wholesale tier columns for this row
                        ProcessWholesaleTierColumns(worksheet, row, wholesaleTierColumns, product);
                        result.Request.Items.Add(product);
                        result.ProcessedRows++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing row {Row}", row);
                    result.Errors.Add(CreateGeneralError(row, ex.Message));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Detects wholesale tier columns by looking for paired "Min Qty [TierName]" and
    /// ("Discount % [TierName]" or "Wholesale Price [TierName]") headers.
    /// Returns a dictionary mapping tier name to (minQtyColumnIndex, valueColumnIndex, isFixedPrice).
    /// </summary>
    private Dictionary<string, (int MinQtyCol, int ValueCol, bool IsFixedPrice)> DetectWholesaleTierColumns(ExcelWorksheet worksheet)
    {
        var tierColumns = new Dictionary<string, (int MinQtyCol, int ValueCol, bool IsFixedPrice)>();
        if (worksheet.Dimension == null) return tierColumns;

        var minQtyPrefixStr = (string)_localizer["Min Qty"];
        var discountPrefixStr = (string)_localizer["Discount %"];
        var fixedPricePrefixStr = (string)_localizer["Wholesale Price"];
        var minQtyPrefix = NormalizeColumnHeader(minQtyPrefixStr);
        var discountPrefix = NormalizeColumnHeader(discountPrefixStr);
        var fixedPricePrefix = NormalizeColumnHeader(fixedPricePrefixStr);

        // Collect all min qty, discount and fixed-price columns with their tier names
        var minQtyCols = new Dictionary<string, int>(); // tierName -> colIndex
        var discountCols = new Dictionary<string, int>(); // tierName -> colIndex
        var fixedPriceCols = new Dictionary<string, int>(); // tierName -> colIndex

        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text?.Trim();
            if (string.IsNullOrEmpty(headerValue)) continue;

            var normalizedHeader = NormalizeColumnHeader(headerValue);

            if (normalizedHeader.StartsWith(minQtyPrefix) && normalizedHeader.Length > minQtyPrefix.Length)
            {
                var tierName = headerValue.Substring(minQtyPrefixStr.Length).Trim();
                if (!string.IsNullOrEmpty(tierName))
                    minQtyCols[tierName] = col;
            }
            else if (normalizedHeader.StartsWith(discountPrefix) && normalizedHeader.Length > discountPrefix.Length)
            {
                var tierName = headerValue.Substring(discountPrefixStr.Length).Trim();
                if (!string.IsNullOrEmpty(tierName))
                    discountCols[tierName] = col;
            }
            else if (normalizedHeader.StartsWith(fixedPricePrefix) && normalizedHeader.Length > fixedPricePrefix.Length)
            {
                var tierName = headerValue.Substring(fixedPricePrefixStr.Length).Trim();
                if (!string.IsNullOrEmpty(tierName))
                    fixedPriceCols[tierName] = col;
            }
        }

        // Match pairs: only include tiers that have BOTH min qty and a value column
        foreach (var tierName in minQtyCols.Keys)
        {
            if (fixedPriceCols.TryGetValue(tierName, out var fixedPriceCol))
            {
                tierColumns[tierName] = (minQtyCols[tierName], fixedPriceCol, true);
            }
            else if (discountCols.TryGetValue(tierName, out var discountCol))
            {
                tierColumns[tierName] = (minQtyCols[tierName], discountCol, false);
            }
        }

        if (tierColumns.Count > 0)
        {
            _logger.LogInformation("Detected {Count} wholesale tier columns: {Tiers}",
                tierColumns.Count, string.Join(", ", tierColumns.Keys));
        }

        return tierColumns;
    }

    /// <summary>
    /// Processes wholesale tier columns for a single product row.
    /// </summary>
    private void ProcessWholesaleTierColumns(
        ExcelWorksheet worksheet,
        int row,
        Dictionary<string, (int MinQtyCol, int ValueCol, bool IsFixedPrice)> tierColumns,
        ProductBulkLoadDto product)
    {
        foreach (var (tierName, cols) in tierColumns)
        {
            var minQtyResult = GetCellValueAsDecimal(worksheet, row, cols.MinQtyCol, $"Min Qty {tierName}");
            var valueResult = GetCellValueAsDecimal(worksheet, row, cols.ValueCol,
                cols.IsFixedPrice ? $"Wholesale Price {tierName}" : $"Discount % {tierName}");

            var minQty = minQtyResult.IsSuccess ? minQtyResult.Value : 0m;
            var value = valueResult.IsSuccess ? valueResult.Value : 0m;

            // Only add if at least one value is meaningful
            if (minQty > 0 || value > 0)
            {
                if (cols.IsFixedPrice)
                    product.WholesalePrices[tierName] = (minQty, 0m, value);
                else
                    product.WholesalePrices[tierName] = (minQty, value, null);
            }
        }
    }

    /// <summary>
    /// Builds a mapping from normalized column names to their Excel column indices
    /// Think of this as creating a translation dictionary between Excel columns and our properties
    /// </summary>
    private Dictionary<string, int> BuildColumnMapping(ExcelWorksheet worksheet)
    {
        var columnMapping = new Dictionary<string, int>();

        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                var normalizedHeader = NormalizeColumnHeader(headerValue);
                if (!columnMapping.ContainsKey(normalizedHeader))
                {
                    columnMapping[normalizedHeader] = col;
                }
            }
        }

        return columnMapping;
    }

    /// <summary>
    /// Processes a single Excel row and converts it to a ProductBulkLoadDto
    /// This is like carefully examining each item in a shipment and converting it to our inventory format
    /// </summary>
    private ProductBulkLoadDto? ProcessDataRow(
        ExcelWorksheet worksheet, 
        int row, 
        Dictionary<string, int> columnMapping, 
        List<ExcelError> errors)
    {
        var record = new ProductBulkLoadDto();

        try
        {
            // Map each column using our configuration-driven approach
            var columnConfigs = ProductTemplateColumns.GetProductColumnConfigurations(_localizer);
            
            foreach (var config in columnConfigs)
            {
                var normalizedPropertyName = NormalizeColumnHeader(config.GetLocalizedName());
                
                if (!columnMapping.TryGetValue(normalizedPropertyName, out int columnIndex))
                {
                    if (config.IsRequired)
                    {
                        errors.Add(CreateColumnError(row, config.GetLocalizedName(), 
                            _localizer["{0} column not found", config.GetLocalizedName()], $"Row{row}"));
                    }
                    continue;
                }

                var success = MapCellValueToProperty(worksheet, row, columnIndex, config, record, errors);
                if (!success && config.IsRequired)
                {
                    return null; // Stop processing this row if required field fails
                }
            }

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing row {Row}", row);
            errors.Add(CreateGeneralError(row, ex.Message));
            return null;
        }
    }

    /// <summary>
    /// Maps a single Excel cell value to the corresponding property in our ProductBulkLoadDto
    /// This is like a specialized converter for each type of data we expect
    /// </summary>
    private bool MapCellValueToProperty(
        ExcelWorksheet worksheet, 
        int row, 
        int columnIndex, 
        ProductColumnConfig config, 
        ProductBulkLoadDto record, 
        List<ExcelError> errors)
    {
        try
        {
            var cellValue = GetCellValueAsString(worksheet, row, columnIndex);
            var cellReference = GetCellReference(row, columnIndex);

            // Handle different property types using reflection and type-safe conversion
            switch (config.PropertyName)
            {
                case "Code":
                    record.Code = cellValue;
                    break;
                case "Name":
                    record.Name = cellValue;
                    return ValidateRequiredString(cellValue, config, row, errors, cellReference);
                case "Brand":
                    record.Brand = cellValue;
                    return ValidateRequiredString(cellValue, config, row, errors, cellReference);
                case "Description":
                    record.Description = cellValue;
                    return ValidateRequiredString(cellValue, config, row, errors, cellReference);
                case "Barcode":
                    record.Barcode = string.IsNullOrWhiteSpace(cellValue) ? null : cellValue;
                    break;
                case "Content":
                    return MapDecimalProperty(worksheet, row, columnIndex, config, 
                        value => record.Content = value, errors, cellReference, validatePositive: true);
                case "UnitMeasureCode":
                    record.UnitMeasureCode = cellValue;
                    return ValidateRequiredString(cellValue, config, row, errors, cellReference);
                case "Cost":
                    return MapDecimalProperty(worksheet, row, columnIndex, config,
                        value => record.Cost = value, errors, cellReference, validatePositive: false);
                case "Price":
                    return MapDecimalProperty(worksheet, row, columnIndex, config,
                        value => record.Price = value, errors, cellReference, validatePositive: true);
                case "IsTaxable":
                    return MapBooleanProperty(worksheet, row, columnIndex, config, 
                        value => record.IsTaxable = value, errors, cellReference);
                case "IsActive":
                    return MapBooleanProperty(worksheet, row, columnIndex, config, 
                        value => record.IsActive = value, errors, cellReference);
                case "MexicoProductServiceCode":
                    record.MexicoProductServiceCode = string.IsNullOrWhiteSpace(cellValue) ? null : cellValue;
                    break;
                case "AllowPartialSale":
                    return MapBooleanProperty(worksheet, row, columnIndex, config,
                        value => record.AllowPartialSale = value, errors, cellReference);
                case "AllowCustomPricing":
                    return MapBooleanProperty(worksheet, row, columnIndex, config,
                        value => record.AllowCustomPricing = value, errors, cellReference);
                default:
                    _logger.LogWarning("Unknown property name: {PropertyName}", config.PropertyName);
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error mapping property {PropertyName} for row {Row}", config.PropertyName, row);
            errors.Add(CreateColumnError(row, config.GetLocalizedName(), ex.Message, GetCellReference(row, columnIndex)));
            return false;
        }
    }

    /// <summary>
    /// Maps a decimal value from Excel cell to a property, with validation
    /// </summary>
    private bool MapDecimalProperty(
        ExcelWorksheet worksheet, 
        int row, 
        int columnIndex, 
        ProductColumnConfig config,
        Action<decimal> setProperty, 
        List<ExcelError> errors, 
        string cellReference,
        bool validatePositive = false)
    {
        var decimalResult = GetCellValueAsDecimal(worksheet, row, columnIndex, config.GetLocalizedName());
        if (!decimalResult.IsSuccess)
        {
            errors.Add(CreateColumnError(row, config.GetLocalizedName(), decimalResult.Error!, cellReference));
            return false;
        }

        if (validatePositive && decimalResult.Value <= 0)
        {
            errors.Add(CreateColumnError(row, config.GetLocalizedName(), 
                _localizer["{0} must be greater than 0", config.GetLocalizedName()], cellReference));
            return false;
        }

        setProperty(decimalResult.Value);
        return true;
    }

    /// <summary>
    /// Maps a boolean value from Excel cell to a property
    /// </summary>
    private bool MapBooleanProperty(
        ExcelWorksheet worksheet, 
        int row, 
        int columnIndex, 
        ProductColumnConfig config,
        Action<bool> setProperty, 
        List<ExcelError> errors, 
        string cellReference)
    {
        var boolResult = GetCellValueAsBoolean(worksheet, row, columnIndex);
        if (!boolResult.IsSuccess)
        {
            errors.Add(CreateColumnError(row, config.GetLocalizedName(), boolResult.Error!, cellReference));
            return false;
        }

        setProperty(boolResult.Value);
        return true;
    }

    /// <summary>
    /// Validates that a required string field has a value
    /// </summary>
    private bool ValidateRequiredString(
        string value, 
        ProductColumnConfig config, 
        int row, 
        List<ExcelError> errors, 
        string cellReference)
    {
        if (config.IsRequired && string.IsNullOrWhiteSpace(value))
        {
            errors.Add(CreateColumnError(row, config.GetLocalizedName(), 
                _localizer["{0} is required", config.GetLocalizedName()], cellReference));
            return false;
        }
        return true;
    }

    private string GetCellValueAsString(ExcelWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cells[row, col];
        return cell.Text?.Trim() ?? "";
    }

    private Result<decimal> GetCellValueAsDecimal(ExcelWorksheet worksheet, int row, int col, string fieldName)
    {
        var cell = worksheet.Cells[row, col];
        
        if (cell.Value == null || string.IsNullOrWhiteSpace(cell.Text))
        {
            return Result<decimal>.Success(0m);
        }
        
        // Try different numeric types that Excel might use
        if (cell.Value is decimal decValue)
            return Result<decimal>.Success(decValue);
        
        if (cell.Value is double doubleValue)
            return Result<decimal>.Success((decimal)doubleValue);
        
        if (cell.Value is int intValue)
            return Result<decimal>.Success(intValue);
        
        // Try parsing as string with culture-aware formatting
        if (decimal.TryParse(cell.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
            return Result<decimal>.Success(result);
        
        return Result<decimal>.Failure(
            _localizer["Invalid decimal format in {0}. Expected numeric value, received: '{1}'", fieldName, cell.Text]);
    }

    private Result<bool> GetCellValueAsBoolean(ExcelWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cells[row, col];
        
        if (cell.Value == null || string.IsNullOrWhiteSpace(cell.Text))
        {
            return Result<bool>.Success(false);
        }
        
        if (cell.Value is bool boolValue)
            return Result<bool>.Success(boolValue);
        
        var text = cell.Text.Trim().ToLowerInvariant();
        
        return text switch
        {
            "true" or "1" or "yes" or "y" or "si" or "s" or "verdadero" => Result<bool>.Success(true),
            "false" or "0" or "no" or "n" or "falso" => Result<bool>.Success(false),
            _ => Result<bool>.Failure(
                _localizer["Invalid boolean format. Expected: true/false, 1/0, yes/no, received: '{0}'", text])
        };
    }

    /// <summary>
    /// Normalizes column headers to handle multiple languages, accents, and formatting variations
    /// This is like creating a universal translator for column names
    /// </summary>
    private string NormalizeColumnHeader(string header)
    {
        var normalized = header.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");
        
        normalized = RemoveAccents(normalized);
        return normalized.ToUpper();
    }

    /// <summary>
    /// Removes accents and diacritics from text to enable fuzzy matching of column names
    /// </summary>
    private string RemoveAccents(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Converts Excel row and column indices to Excel-style cell reference (A1, B2, etc.)
    /// </summary>
    private string GetCellReference(int row, int col)
    {
        var columnLetter = "";
        while (col > 0)
        {
            col--;
            columnLetter = (char)('A' + col % 26) + columnLetter;
            col /= 26;
        }
        return $"{columnLetter}{row}";
    }

    /// <summary>
    /// Creates a standardized error object for column-specific validation errors
    /// </summary>
    private ExcelError CreateColumnError(int row, string columnName, string errorMessage, string cellReference)
    {
        return new ExcelError
        {
            RowNumber = row,
            ColumnName = columnName,
            CellValue = "",
            ErrorMessage = errorMessage,
            CellReference = cellReference
        };
    }

    /// <summary>
    /// Creates a standardized error object for general row processing errors
    /// </summary>
    private ExcelError CreateGeneralError(int row, string errorMessage)
    {
        return new ExcelError
        {
            RowNumber = row,
            ColumnName = _localizer["General error"],
            CellValue = "",
            ErrorMessage = _localizer["Error processing row {0}: {1}", row, errorMessage],
            CellReference = $"Row{row}"
        };
    }
}