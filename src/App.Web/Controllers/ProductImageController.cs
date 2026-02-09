using App.Core.Common;
using App.Core.Constants;
using App.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

/// <summary>
/// Controller for handling product image delivery
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductImageController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductImageController> _logger;

    public ProductImageController(
        IProductService productService,
        ILogger<ProductImageController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the primary image for a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image file result</returns>
    [HttpGet("{id:long}/primary")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewProducts)]
    public async Task<IActionResult> GetPrimaryImage(
        long id, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetProductImageDataAsync(id, false, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return NotFound(result.Error);
            }

            var (imageData, contentType, fileName) = result.Value!;
            
            return File(
                imageData, 
                contentType, 
                fileName,
                enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving primary image for product {ProductId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the thumbnail image for a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thumbnail image file result</returns>
    [HttpGet("{id:long}/thumbnail")]
    [Authorize(Policy = ApplicationClaims.Shop.ViewProducts)]
    public async Task<IActionResult> GetThumbnail(
        long id, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetProductImageDataAsync(id, true, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return NotFound(result.Error);
            }

            var (imageData, contentType, fileName) = result.Value!;
            
            return File(
                imageData, 
                contentType, 
                fileName,
                enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thumbnail for product {ProductId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Internal method to get image data for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="isThumbnail">Whether to get thumbnail or primary image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing image data, content type, and filename</returns>
    private async Task<Result<(byte[] ImageData, string ContentType, string FileName)>> GetProductImageDataAsync(
        long productId, 
        bool isThumbnail, 
        CancellationToken cancellationToken)
    {
        // Get product images
        var images = await _productService.GetProductImagesAsync(productId, cancellationToken);
        
        if (!images.Any())
        {
            return Result<(byte[], string, string)>.Failure("No images found for this product");
        }

        // Get primary image
        var primaryImage = images.FirstOrDefault(x => x.IsPrimary) ?? images.First();
        
        // Determine which image data to return
        var fileName = isThumbnail 
            ? primaryImage.ThumbnailFileName ?? primaryImage.FileName
            : primaryImage.FileName;

        // For this implementation, we'll need to add a method to get image binary data
        // This would require extending the IProductService interface
        var imageDataResult = await GetImageBinaryDataAsync(primaryImage.Id, isThumbnail, cancellationToken);
        
        if (!imageDataResult.IsSuccess)
        {
            return Result<(byte[], string, string)>.Failure(imageDataResult.Error!);
        }

        return Result<(byte[], string, string)>.Success((
            imageDataResult.Value!, 
            primaryImage.ContentType, 
            fileName));
    }

    /// <summary>
    /// Gets binary image data from the database
    /// </summary>
    private async Task<Result<byte[]>> GetImageBinaryDataAsync(
        long imageId, 
        bool isThumbnail, 
        CancellationToken cancellationToken)
    {
        return await _productService.GetProductImageBinaryDataAsync(imageId, isThumbnail, cancellationToken);
    }
}