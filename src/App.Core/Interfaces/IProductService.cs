using App.Core.Common;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Product;

namespace App.Core.Interfaces;

public interface IProductService
{
    /// <summary>
    /// Gets a paginated list of products
    /// </summary>
    Task<(int TotalCount, IList<ProductDto> Items)> GetProductsAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        int? unitMeasureId = null,
        bool? isActive = null,
        bool? isPartialSaleAllowed = null);

    /// <summary>
    /// Gets a product by barcode
    /// </summary>
    Task<ProductDto?> GetProductByBarcodeAsync(string barcode);

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    Task<ProductDto?> GetProductByIdAsync(long id);

    /// <summary>
    /// Gets a product by code
    /// </summary>
    Task<ProductDto?> GetProductByCodeAsync(string code);

    /// <summary>
    /// Creates a new product
    /// </summary>
    Task<ProductDto> CreateProductAsync(CreateProductDto createDto);

    /// <summary>
    /// Updates an existing product
    /// </summary>
    Task<ProductDto> UpdateProductAsync(long id, UpdateProductDto updateDto);

    /// <summary>
    /// Soft deletes a product
    /// </summary>
    Task<bool> DeleteProductAsync(long id);

    /// <summary>
    /// Adds an image to a product
    /// </summary>
    Task<ProductImageDto> AddProductImageAsync(
        long productId,
        Stream imageStream,
        string fileName,
        string contentType,
        bool isPrimary = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all images for a product
    /// </summary>
    Task<IList<ProductImageDto>> GetProductImagesAsync(
        long productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an image from a product
    /// </summary>
    Task<bool> RemoveProductImageAsync(
        long productId,
        long imageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an image as the primary image for a product
    /// </summary>
    Task<bool> SetPrimaryImageAsync(
        long productId,
        long imageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a product code is unique
    /// </summary>
    Task<bool> ValidateUniqueCodeAsync(string code, long? excludeId = null);

    /// <summary>
    /// Validates that a product barcode is unique
    /// </summary>
    Task<bool> ValidateUniqueBarcodeAsync(string barcode, long? excludeId = null);

    Task<Result<List<ProductBulkLoadResultDto>>> CreateBulkProductsAsync(
        BulkProductLoadRequestDto request,
        CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Gets binary image data for a product image
    /// </summary>
    /// <param name="imageId">Product image ID</param>
    /// <param name="isThumbnail">Whether to get thumbnail or primary image data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing binary image data</returns>
    Task<Result<byte[]>> GetProductImageBinaryDataAsync(
        long imageId, 
        bool isThumbnail = false, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific product image by ID
    /// </summary>
    /// <param name="imageId">Product image ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product image DTO or null if not found</returns>
    Task<ProductImageDto?> GetProductImageByIdAsync(
        long imageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all products to Excel format
    /// </summary>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportCatalogAsync();
}