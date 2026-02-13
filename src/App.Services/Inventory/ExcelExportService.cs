using System.Drawing;
using System.Globalization;

using AutoMapper;

using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Product;
using App.Core.DTOs.Shop;
using App.Core.Interfaces;
using App.Models.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace App.Services.Inventory;

public class ExcelExportService : IExcelExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ExcelExportService> _logger;
    private readonly IStringLocalizer<ExcelExportService> _localizer;

    public ExcelExportService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<ExcelExportService> logger,
        IStringLocalizer<ExcelExportService> localizer)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
    }

    private IStringLocalizer<ExcelExportService> L => _localizer;

    public async Task<byte[]> ExportInventoryToExcelAsync(
        IList<InventoryDto> items,        
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(L["Inventory"]);

        // Add headers
        worksheet.Cells[1, 1].Value = L["Code"];
        worksheet.Cells[1, 2].Value = L["Name"];
        worksheet.Cells[1, 3].Value = L["Brand"];
        worksheet.Cells[1, 4].Value = L["Description"];
        worksheet.Cells[1, 5].Value = L["Warehouse"];
        worksheet.Cells[1, 6].Value = L["Stock"];
        worksheet.Cells[1, 7].Value = L["Min Stock"];
        worksheet.Cells[1, 8].Value = L["Max Stock"];
        worksheet.Cells[1, 9].Value = L["Unit"];
        worksheet.Cells[1, 10].Value = L["Content Per Unit"];
        worksheet.Cells[1, 11].Value = L["Status"];

        // Format header row
        using (var range = worksheet.Cells[1, 1, 1, 11])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 237, 237));
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        // Get number format based on culture
        var numberFormat = GetNumberFormat(culture);

        // Add data
        var row = 2;
        foreach (var item in items)
        {
            worksheet.Cells[row, 1].Value = item.ProductCode;
            worksheet.Cells[row, 2].Value = item.ProductName;
            worksheet.Cells[row, 3].Value = item.ProductBrand;
            worksheet.Cells[row, 4].Value = item.ProductDescription;
            worksheet.Cells[row, 5].Value = item.WarehouseName;
            worksheet.Cells[row, 6].Value = item.Quantity;
            worksheet.Cells[row, 7].Value = item.MinStock;
            worksheet.Cells[row, 8].Value = item.MaxStock;
            worksheet.Cells[row, 9].Value = item.UnitMeasureName;

            // Content Per Unit
            if (item.ProductContent > 0)
            {
                worksheet.Cells[row, 10].Value = $"{item.ProductContent} {item.UnitMeasureName}";
            }
            else
            {
                worksheet.Cells[row, 10].Value = "-";
            }

            // Set status
            var status = GetStockStatus(item);
            worksheet.Cells[row, 11].Value = status.Text;

            // Format number columns with culture-specific format
            worksheet.Cells[row, 6].Style.Numberformat.Format = numberFormat;
            if (item.MinStock.HasValue)
                worksheet.Cells[row, 7].Style.Numberformat.Format = numberFormat;
            if (item.MaxStock.HasValue)
                worksheet.Cells[row, 8].Style.Numberformat.Format = numberFormat;

            // Color status cell based on status
            var statusCell = worksheet.Cells[row, 11];
            statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            statusCell.Style.Fill.BackgroundColor.SetColor(GetStatusColor(status.Color));

            row++;
        }

        // Add borders
        var dataRange = worksheet.Cells[1, 1, row - 1, 11];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        // Set column widths based on content while considering culture
        worksheet.Cells.AutoFitColumns();

        // Adjust numeric columns to ensure enough width for formatted numbers
        worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, GetMinColumnWidth(culture));
        worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, GetMinColumnWidth(culture));
        worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, GetMinColumnWidth(culture));
        worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, GetMinColumnWidth(culture));
        worksheet.Column(8).Width = Math.Max(worksheet.Column(8).Width, GetMinColumnWidth(culture));
        worksheet.Column(9).Width = Math.Max(worksheet.Column(9).Width, GetMinColumnWidth(culture));
        worksheet.Column(10).Width = Math.Max(worksheet.Column(10).Width, GetMinColumnWidth(culture));
        worksheet.Column(11).Width = Math.Max(worksheet.Column(11).Width, GetMinColumnWidth(culture));

        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> ExportMovementHistoryToExcelAsync(
        IList<InventoryMovementDto> items,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(L["Movement History"]);

        // Add headers
        worksheet.Cells[1, 1].Value = L["Date"];
        worksheet.Cells[1, 2].Value = L["Product"];
        worksheet.Cells[1, 3].Value = L["Brand"];
        worksheet.Cells[1, 4].Value = L["Description"];
        worksheet.Cells[1, 5].Value = L["Unit"];
        worksheet.Cells[1, 6].Value = L["Content Per Unit"];
        worksheet.Cells[1, 7].Value = L["Type"];
        worksheet.Cells[1, 8].Value = L["Source"];
        worksheet.Cells[1, 9].Value = L["Destination"];
        worksheet.Cells[1, 10].Value = L["Quantity"];
        worksheet.Cells[1, 11].Value = L["Previous Balance"];
        worksheet.Cells[1, 12].Value = L["New Balance"];
        worksheet.Cells[1, 13].Value = L["Reference"];
        worksheet.Cells[1, 14].Value = L["Reason"];

        // Format header row
        using (var range = worksheet.Cells[1, 1, 1, 14])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 237, 237));
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        // Get number format based on culture
        var numberFormat = GetNumberFormat(culture);

        // Add data
        var row = 2;
        foreach (var item in items)
        {
            worksheet.Cells[row, 1].Value = item.MovementDate.ToLocalTime();
            worksheet.Cells[row, 2].Value = $"{item.ProductName} ({item.ProductCode})";
            worksheet.Cells[row, 3].Value = item.BrandName;
            worksheet.Cells[row, 4].Value = item.ProductDescription ?? L["N/A"];

            // Unit
            worksheet.Cells[row, 5].Value = item.UnitMeasureName;

            // Content Per Unit
            if (item.ProductContent > 0)
            {
                worksheet.Cells[row, 6].Value = $"{item.ProductContent} {item.UnitMeasureName}";
            }
            else
            {
                worksheet.Cells[row, 6].Value = "-";
            }

            worksheet.Cells[row, 7].Value = item.MovementType;
            worksheet.Cells[row, 8].Value = item.WarehouseName;
            worksheet.Cells[row, 9].Value = item.DestinationWarehouseName;
            worksheet.Cells[row, 10].Value = item.Quantity;
            worksheet.Cells[row, 11].Value = item.PreviousBalance;
            worksheet.Cells[row, 12].Value = item.NewBalance;
            worksheet.Cells[row, 13].Value = item.Reference;
            worksheet.Cells[row, 14].Value = item.Reason;

            // Format number columns
            worksheet.Cells[row, 10].Style.Numberformat.Format = numberFormat;
            worksheet.Cells[row, 11].Style.Numberformat.Format = numberFormat;
            worksheet.Cells[row, 12].Style.Numberformat.Format = numberFormat;

            row++;
        }

        // Add borders
        var dataRange = worksheet.Cells[1, 1, row - 1, 14];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> ExportAlertsToExcelAsync(
        IList<InventoryAlertDto> items,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(L["Inventory Alerts"]);

        // Add headers
        worksheet.Cells[1, 1].Value = L["Product Code"];
        worksheet.Cells[1, 2].Value = L["Product Name"];
        worksheet.Cells[1, 3].Value = L["Brand"];
        worksheet.Cells[1, 4].Value = L["Description"];
        worksheet.Cells[1, 5].Value = L["Warehouse"];
        worksheet.Cells[1, 6].Value = L["Alert Type"];
        worksheet.Cells[1, 7].Value = L["Current Stock"];
        worksheet.Cells[1, 8].Value = L["Min Stock"];
        worksheet.Cells[1, 9].Value = L["Max Stock"];
        worksheet.Cells[1, 10].Value = L["Unit"];

        // Format header row
        using (var range = worksheet.Cells[1, 1, 1, 10])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 237, 237));
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        // Get number format based on culture
        var numberFormat = GetNumberFormat(culture);

        // Add data
        var row = 2;
        foreach (var item in items)
        {
            worksheet.Cells[row, 1].Value = item.ProductCode;
            worksheet.Cells[row, 2].Value = item.ProductName;
            worksheet.Cells[row, 3].Value = item.ProductBrand;
            worksheet.Cells[row, 4].Value = item.ProductDescription ?? L["N/A"];
            worksheet.Cells[row, 5].Value = item.WarehouseName;

            // Alert type with localization
            worksheet.Cells[row, 6].Value = item.AlertType == InventoryAlertType.LowStock ? 
                L["Low Stock"] : L["Over Stock"];
            
            // Format color for alert type cell
            var alertCell = worksheet.Cells[row, 6];
            alertCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            
            if (item.AlertType == InventoryAlertType.LowStock)
            {
                alertCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 243, 224)); // Light orange for low stock
            }
            else
            {
                alertCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(232, 245, 233)); // Light green for over stock
            }
            
            worksheet.Cells[row, 7].Value = item.CurrentStock;
            worksheet.Cells[row, 8].Value = item.MinStock;
            worksheet.Cells[row, 9].Value = item.MaxStock;
            worksheet.Cells[row, 10].Value = item.UnitMeasureName;

            // Format number columns with culture-specific format
            worksheet.Cells[row, 7].Style.Numberformat.Format = numberFormat;
            if (item.MinStock.HasValue)
                worksheet.Cells[row, 8].Style.Numberformat.Format = numberFormat;
            if (item.MaxStock.HasValue)
                worksheet.Cells[row, 9].Style.Numberformat.Format = numberFormat;

            row++;
        }

        // Add summary section
        var summaryRow = row + 1;
        worksheet.Cells[summaryRow, 1].Value = L["Summary"];
        worksheet.Cells[summaryRow, 1].Style.Font.Bold = true;
        
        worksheet.Cells[summaryRow + 1, 1].Value = L["Total Alerts"];
        worksheet.Cells[summaryRow + 1, 2].Value = items.Count;
        
        worksheet.Cells[summaryRow + 2, 1].Value = L["Low Stock Alerts"];
        worksheet.Cells[summaryRow + 2, 2].Value = items.Count(x => x.AlertType == InventoryAlertType.LowStock);
        
        worksheet.Cells[summaryRow + 3, 1].Value = L["Over Stock Alerts"];
        worksheet.Cells[summaryRow + 3, 2].Value = items.Count(x => x.AlertType == InventoryAlertType.OverStock);
        
        worksheet.Cells[summaryRow + 4, 1].Value = L["Generated On"];
        worksheet.Cells[summaryRow + 4, 2].Value = DateTime.Now.ToString(culture);

        // Add borders
        var dataRange = worksheet.Cells[1, 1, row - 1, 10];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        // Set column widths based on content while considering culture
        worksheet.Cells.AutoFitColumns();

        // Ensure minimum widths for numeric columns
        worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, GetMinColumnWidth(culture));
        worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, GetMinColumnWidth(culture));
        worksheet.Column(7).Width = Math.Max(worksheet.Column(7).Width, GetMinColumnWidth(culture));
        worksheet.Column(8).Width = Math.Max(worksheet.Column(8).Width, GetMinColumnWidth(culture));
        worksheet.Column(9).Width = Math.Max(worksheet.Column(9).Width, GetMinColumnWidth(culture));
        worksheet.Column(10).Width = Math.Max(worksheet.Column(10).Width, GetMinColumnWidth(culture));

        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    private string GetNumberFormat(CultureInfo culture)
    {
        var numberFormat = culture.NumberFormat;
        var decimalSeparator = numberFormat.NumberDecimalSeparator;
        var groupSeparator = numberFormat.NumberGroupSeparator;

        // Build format string based on culture's number format
        // Example: For en-US: #,##0.00, for es-ES: #.##0,00
        return $"#{groupSeparator}##0{decimalSeparator}00";
    }

    private double GetMinColumnWidth(CultureInfo culture)
    {
        // Calculate minimum width based on typical number format length
        // Consider group separator, decimal separator, and typical number of digits
        var baseWidth = 8.0; // Base width for digits
        var format = culture.NumberFormat;
        
        // Add extra width for group separator if used
        if (!string.IsNullOrEmpty(format.NumberGroupSeparator))
            baseWidth += 1.5;
            
        // Add extra width for negative numbers
        baseWidth += 1.5;

        return baseWidth;
    }

    private (string Text, Color Color) GetStockStatus(InventoryDto inventory)
    {
        if (inventory.Quantity == 0)
            return (L["Out of Stock"], Color.FromArgb(255, 235, 238));

        if (inventory.MinStock.HasValue && inventory.Quantity < inventory.MinStock.Value)
            return (L["Low Stock"], Color.FromArgb(255, 243, 224));

        if (inventory.MaxStock.HasValue && inventory.Quantity > inventory.MaxStock.Value)
            return (L["Over Stock"], Color.FromArgb(232, 245, 233));

        return (L["In Stock"], Color.FromArgb(237, 247, 237));
    }

    private Color GetStatusColor(Color color)
    {
        return Color.FromArgb(color.R, color.G, color.B);
    }

    public async Task<byte[]> ExportProductCatalogToExcelAsync(
        IList<ProductDto> items,
        CultureInfo culture,
        IList<FractionColumnDto>? fractions = null,
        IList<ProductSurchargeExportDto>? surcharges = null,
        IList<WholesaleTierColumnDto>? wholesaleTiers = null,
        IList<ProductWholesaleExportDto>? wholesalePrices = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Allow cancellation check

        using var package = new ExcelPackage();
        package.Workbook.Properties.Title = L["Product Catalog"];
        package.Workbook.Properties.Company = "App System";

        var worksheet = package.Workbook.Worksheets.Add(L["Product Catalog"]);

        // Build surcharge lookup dictionary: ProductId -> FractionCode -> SurchargePercentage
        var surchargeDict = new Dictionary<long, Dictionary<string, decimal>>();
        if (surcharges != null)
        {
            foreach (var s in surcharges)
            {
                if (!surchargeDict.ContainsKey(s.ProductId))
                    surchargeDict[s.ProductId] = new Dictionary<string, decimal>();
                surchargeDict[s.ProductId][s.FractionCode] = s.SurchargePercentage;
            }
        }

        // Build wholesale lookup dictionary: ProductId -> TierName -> (MinQuantity, DiscountPercentage)
        var wholesaleDict = new Dictionary<long, Dictionary<string, (decimal MinQuantity, decimal DiscountPercentage)>>();
        if (wholesalePrices != null)
        {
            foreach (var wp in wholesalePrices)
            {
                if (!wholesaleDict.ContainsKey(wp.ProductId))
                    wholesaleDict[wp.ProductId] = new Dictionary<string, (decimal, decimal)>();
                wholesaleDict[wp.ProductId][wp.TierName] = (wp.MinQuantity, wp.DiscountPercentage);
            }
        }

        // Base headers
        var baseHeaders = new List<string>
        {
            L["Code"],
            L["Name"],
            L["Brand"],
            L["Description"],
            L["Barcode"],
            L["Content"],
            L["Unit Measure"],
            L["Cost"],
            L["Price"],
            L["Is Taxable"],
            L["Is Active"],
            L["Allow Partial Sale"],
            L["Allow Custom Pricing"]
        };

        // Add fraction columns if there are fractions
        var fractionList = fractions?.ToList() ?? new List<FractionColumnDto>();
        foreach (var fraction in fractionList)
        {
            baseHeaders.Add($"{L["Surcharge"]} {fraction.Code}");
        }

        // Track where wholesale columns start
        var wholesaleTierList = wholesaleTiers?.ToList() ?? new List<WholesaleTierColumnDto>();
        var wholesaleColumnStartIndex = baseHeaders.Count; // 0-based index
        foreach (var tier in wholesaleTierList)
        {
            baseHeaders.Add($"{L["Min Qty"]} {tier.Name}");
            baseHeaders.Add($"{L["Discount %"]} {tier.Name}");
        }

        var headers = baseHeaders.ToArray();
        var surchargeColumnStartIndex = 13; // 0-based index where surcharge columns start

        // Set headers with styling
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cells[1, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

            // Use different colors for different column groups
            if (i >= wholesaleColumnStartIndex)
            {
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(39, 174, 96)); // Green for wholesale
            }
            else if (i >= surchargeColumnStartIndex)
            {
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(155, 89, 182)); // Purple for surcharges
            }
            else
            {
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189)); // Blue for base
            }

            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        // Data rows
        for (int i = 0; i < items.Count; i++)
        {
            var product = items[i];
            var row = i + 2;

            worksheet.Cells[row, 1].Value = product.Code;
            worksheet.Cells[row, 2].Value = product.Name;
            worksheet.Cells[row, 3].Value = product.Brand;
            worksheet.Cells[row, 4].Value = product.Description;
            worksheet.Cells[row, 5].Value = product.Barcode;
            worksheet.Cells[row, 6].Value = product.Content;
            worksheet.Cells[row, 7].Value = product.UnitMeasureName;

            // Cost with currency formatting
            var costCell = worksheet.Cells[row, 8];
            costCell.Value = product.Cost;
            costCell.Style.Numberformat.Format = GetCurrencyFormat(culture);

            // Price with currency formatting
            var priceCell = worksheet.Cells[row, 9];
            priceCell.Value = product.Price;
            priceCell.Style.Numberformat.Format = GetCurrencyFormat(culture);

            // Boolean values with localized text
            worksheet.Cells[row, 10].Value = product.IsTaxable ? L["Yes"] : L["No"];
            worksheet.Cells[row, 11].Value = product.IsActive ? L["Yes"] : L["No"];
            worksheet.Cells[row, 12].Value = product.IsPartialSaleAllowed ? L["Yes"] : L["No"];
            worksheet.Cells[row, 13].Value = product.AllowCustomPricing ? L["Yes"] : L["No"];

            // Add surcharge values for each fraction column
            for (int f = 0; f < fractionList.Count; f++)
            {
                var colIndex = 14 + f; // Start after base columns (13 columns, 1-indexed)
                var fraction = fractionList[f];

                if (product.IsPartialSaleAllowed &&
                    surchargeDict.TryGetValue(product.Id, out var productSurcharges) &&
                    productSurcharges.TryGetValue(fraction.Code, out var surchargePercent))
                {
                    var percentCell = worksheet.Cells[row, colIndex];
                    percentCell.Value = surchargePercent;
                    percentCell.Style.Numberformat.Format = "0.00\"%\"";
                }
                else
                {
                    worksheet.Cells[row, colIndex].Value = "-";
                    worksheet.Cells[row, colIndex].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
            }

            // Add wholesale values for each tier (2 columns per tier: MinQty and Discount%)
            for (int t = 0; t < wholesaleTierList.Count; t++)
            {
                var minQtyColIndex = wholesaleColumnStartIndex + (t * 2) + 1; // 1-indexed
                var discountColIndex = minQtyColIndex + 1;
                var tier = wholesaleTierList[t];

                if (wholesaleDict.TryGetValue(product.Id, out var productWholesale) &&
                    productWholesale.TryGetValue(tier.Name, out var wholesaleData))
                {
                    var minQtyCell = worksheet.Cells[row, minQtyColIndex];
                    minQtyCell.Value = wholesaleData.MinQuantity;
                    minQtyCell.Style.Numberformat.Format = "0.##";

                    var discountCell = worksheet.Cells[row, discountColIndex];
                    discountCell.Value = wholesaleData.DiscountPercentage;
                    discountCell.Style.Numberformat.Format = "0.00\"%\"";
                }
                else
                {
                    worksheet.Cells[row, minQtyColIndex].Value = "-";
                    worksheet.Cells[row, minQtyColIndex].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, discountColIndex].Value = "-";
                    worksheet.Cells[row, discountColIndex].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
            }

            // Apply row styling
            var range = worksheet.Cells[row, 1, row, headers.Length];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            // Alternate row colors
            if (i % 2 == 1)
            {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
            }
        }

        // Auto-fit columns with reasonable limits
        for (int col = 1; col <= headers.Length; col++)
        {
            worksheet.Column(col).AutoFit();

            // Set reasonable min/max widths
            if (worksheet.Column(col).Width < 10)
                worksheet.Column(col).Width = 10;
            if (worksheet.Column(col).Width > 50)
                worksheet.Column(col).Width = 50;
        }

        // Add table formatting
        if (items.Count > 0)
        {
            var tableRange = worksheet.Cells[1, 1, items.Count + 1, headers.Length];
            var table = worksheet.Tables.Add(tableRange, "ProductCatalog");
            table.ShowHeader = true;
            table.ShowFilter = true;
            table.TableStyle = TableStyles.Medium2;
        }

        return package.GetAsByteArray();
    }

    private string GetCurrencyFormat(CultureInfo culture)
    {
        var currencyFormat = culture.NumberFormat;
        var decimalSeparator = currencyFormat.CurrencyDecimalSeparator;
        var groupSeparator = currencyFormat.CurrencyGroupSeparator;
        var currencySymbol = currencyFormat.CurrencySymbol;

        // Build currency format string based on culture
        return $"{currencySymbol} #{groupSeparator}##0{decimalSeparator}00";
    }

    public async Task<Result<byte[]>> ExportProductCatalogToExcelAsync()
    {
        try
        {
            var products = await _context.Products
                .Include(p => p.UnitMeasure)
                .Include(p => p.MexicoProductService)
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Code)
                .ToListAsync();

            var productDtos = _mapper.Map<List<ProductDto>>(products);

            var bytes = await ExportProductCatalogToExcelAsync(productDtos, CultureInfo.CurrentCulture);
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting product catalog to Excel");
            return Result<byte[]>.Failure($"Error exporting product catalog: {ex.Message}");
        }
    }
}