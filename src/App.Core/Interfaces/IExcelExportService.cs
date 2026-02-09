using System.Globalization;

using App.Core.Common;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Product;
using App.Core.DTOs.Shop;

using Microsoft.Extensions.Localization;

namespace App.Core.Interfaces;

public interface IExcelExportService
{
    Task<byte[]> ExportInventoryToExcelAsync(
        IList<InventoryDto> items,
        CultureInfo culture,
        CancellationToken cancellationToken = default);


    Task<byte[]> ExportMovementHistoryToExcelAsync(
        IList<InventoryMovementDto> items,
        CultureInfo culture,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportAlertsToExcelAsync(
        IList<InventoryAlertDto> items,
        CultureInfo culture,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportProductCatalogToExcelAsync(
        IList<ProductDto> items,
        CultureInfo culture,
        IList<FractionColumnDto>? fractions = null,
        IList<ProductSurchargeExportDto>? surcharges = null,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> ExportProductCatalogToExcelAsync();
}