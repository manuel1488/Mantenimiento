using System.Globalization;

using App.Core.Constants;
using App.Core.Interfaces;

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
    private readonly IStringLocalizer<ProductsController> L;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        IStringLocalizer<ProductsController> localizer,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        L = localizer;
        _logger = logger;
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