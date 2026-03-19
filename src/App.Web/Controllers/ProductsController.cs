using System.Globalization;

using App.Core.Constants;
using App.Core.DTOs.Product;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Services.Settings;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace App.Web.Controllers;

/// <summary>
/// Controller for handling product operations
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ITaxRateService _taxRateService;
    private readonly IProductWholesalePriceService _wholesalePriceService;
    private readonly IStringLocalizer<ProductsController> L;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ITaxRateService taxRateService,
        IProductWholesalePriceService wholesalePriceService,
        IStringLocalizer<ProductsController> localizer,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _taxRateService = taxRateService;
        _wholesalePriceService = wholesalePriceService;
        L = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Searches and returns a paginated list of products.
    /// Used by external apps (e.g. AppEtiquetado) to query the product catalog.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = ApplicationClaims.Shop.ViewProducts)]
    public async Task<ActionResult<object>> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isPartialSaleAllowed = null,
        [FromQuery] bool? isLabelingAllowed = null)
    {
        var (totalCount, items) = await _productService.GetProductsAsync(
            page: 1,
            pageSize: pageSize,
            searchString: search,
            isActive: isActive,
            isPartialSaleAllowed: isPartialSaleAllowed,
            isLabelingAllowed: isLabelingAllowed);

        var currentTaxRate = await _taxRateService.GetEffectiveRateAsync("MX");

        var productIds = items.Select(p => p.Id).ToList();
        var wholesaleResult = await _wholesalePriceService.GetWholesalePricesForProductsAsync(productIds);
        var wholesaleMap = wholesaleResult.IsSuccess ? wholesaleResult.Value : new Dictionary<long, IList<App.Core.DTOs.Shop.ProductWholesalePriceDto>>();

        foreach (var item in items)
        {
            item.TaxRate = item.IsTaxable ? currentTaxRate : 0m;
            if (wholesaleMap.TryGetValue(item.Id, out var prices))
                item.WholesalePrices = prices.ToList();
        }

        return Ok(new { items, totalCount });
    }

    /// <summary>
    /// Downloads the complete product catalog as an Excel file
    /// </summary>
    /// <returns>Excel file with all products</returns>
    [HttpGet("catalog/download")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewProducts)]
    public async Task<IActionResult> DownloadCatalog()
    {
        try
        {
            _logger.LogInformation("Starting product catalog export for user {UserId}", User.Identity?.Name);

            var excelBytes = await _productService.ExportCatalogAsync();
            var fileName = $"product-catalog-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";

            _logger.LogInformation("Product catalog export completed successfully. File size: {FileSize} bytes", excelBytes.Length);

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating product catalog export");
            return StatusCode(500, new { error = L["Error generating catalog export"].Value });
        }
    }
}