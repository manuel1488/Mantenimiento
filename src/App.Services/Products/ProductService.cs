using AutoMapper;

using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Product;
using App.Core.DTOs.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;

using OfficeOpenXml;

namespace App.Services.Products;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;
    private readonly IStringLocalizer<ProductService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICompanySettingsService _settingsService;
    private readonly IDateTime _dateTime;
    private readonly IImageService _imageService;
    private readonly IProductCodeGeneratorService _codeGeneratorService;
    private readonly IServiceProvider _serviceProvider;

    public ProductService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ProductService> logger,
        IStringLocalizer<ProductService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IImageService imageService,
        ICompanySettingsService settingsService,
        IProductCodeGeneratorService codeGeneratorService,
        IServiceProvider serviceProvider)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _imageService = imageService;
        _settingsService = settingsService;
        _codeGeneratorService = codeGeneratorService;
        _serviceProvider = serviceProvider;
    }

    private async Task ValidateMexicoProductServiceIdAsync(int? mexicoProductServiceId)
    {
        var settings = await _settingsService.GetSettingsAsync();
        bool isMexico = settings?.CountryCode == CountryCodes.Mexico;

        await using var _context = await _contextFactory.CreateDbContextAsync();

        if (isMexico && mexicoProductServiceId.HasValue)
        {
            var exists = await _context.MexicoProductServices
                .AnyAsync(x => x.Id == mexicoProductServiceId.Value);

            if (!exists)
            {
                throw new InvalidOperationException(
                    _localizer["Invalid SAT product/service code"]);
            }
        }
        else if (!isMexico && mexicoProductServiceId.HasValue)
        {
            // Si no es México pero se está intentando usar un código SAT, lanzar error
            throw new InvalidOperationException(
                _localizer["SAT product/service codes are only applicable for Mexico"]);
        }
    }

    public async Task<(int TotalCount, IList<ProductDto> Items)> GetProductsAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        int? unitMeasureId = null,
        bool? isActive = null,
        bool? isPartialSaleAllowed = null,
        bool? isLabelingAllowed = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Product> query = _context.Products
                .Include(p => p.UnitMeasure)
                .Include(p => p.MexicoProductService)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    x.Code.Contains(searchString) ||
                    (x.Barcode != null && x.Barcode.Contains(searchString)) ||
                    (x.Description != null && x.Description.Contains(searchString)));
            }

            if (unitMeasureId.HasValue)
            {
                query = query.Where(x => x.UnitMeasureId == unitMeasureId.Value);
            }

            // Añadir filtro de isActive
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            if (isPartialSaleAllowed.HasValue)
            {
                query = query.Where(x => x.IsPartialSaleAllowed == isPartialSaleAllowed.Value);
            }

            if (isLabelingAllowed.HasValue)
            {
                query = query.Where(x => x.IsLabelingAllowed == isLabelingAllowed.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<ProductDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var product = await _context.Products
                .Include(p => p.UnitMeasure)
                .Include(p => p.MexicoProductService)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Barcode == barcode && x.IsActive);

            return product != null ? _mapper.Map<ProductDto>(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by barcode {Barcode}", barcode);
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(long id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var product = await _context.Products
                .Include(p => p.UnitMeasure)
                .Include(p => p.MexicoProductService)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return product != null ? _mapper.Map<ProductDto>(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by id {Id}", id);
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var product = await _context.Products
                .Include(p => p.UnitMeasure)
                .Include(p => p.MexicoProductService)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code);

            return product != null ? _mapper.Map<ProductDto>(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by code {Code}", code);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Validar clave SAT si aplica
            await ValidateMexicoProductServiceIdAsync(createDto.MexicoProductServiceId);

            // Check if product with same code already exists
            var exists = await _context.Products
                .AsNoTracking()
                .AnyAsync(x => x.Code == createDto.Code);

            if (exists)
            {
                throw new InvalidOperationException(
                    _localizer["Product with code {0} already exists", createDto.Code]);
            }

            var product = _mapper.Map<Product>(createDto);

            // Set audit fields
            product.CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            product.CreatedAt = _dateTime.Now;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Load related entities for DTO
            await _context.Entry(product)
                .Reference(p => p.UnitMeasure)
                .LoadAsync();

            if (product.MexicoProductServiceId.HasValue)
            {
                await _context.Entry(product)
                    .Reference(p => p.MexicoProductService)
                    .LoadAsync();
            }

            return _mapper.Map<ProductDto>(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            throw;
        }
    }

    public async Task<ProductDto> UpdateProductAsync(long id, UpdateProductDto updateDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Validar clave SAT si aplica
            await ValidateMexicoProductServiceIdAsync(updateDto.MexicoProductServiceId);

            var product = await _context.Products
                .Include(p => p.UnitMeasure)
                .Include(p => p.MexicoProductService)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                throw new InvalidOperationException(
                    _localizer["Product not found with ID {0}", id]);
            }

            // Check if code is being changed and if new one already exists
            if (updateDto.Code != product.Code)
            {
                var exists = await _context.Products
                    .AnyAsync(x => x.Id != id && x.Code == updateDto.Code);

                if (exists)
                {
                    throw new InvalidOperationException(
                        _localizer["Product with code {0} already exists", updateDto.Code]);
                }
            }

            // Update properties
            _mapper.Map(updateDto, product);

            // A retail price drop must not reach or fall below an active wholesale fixed price,
            // which would turn that wholesale "discount" into a surcharge and corrupt totals.
            var conflictingWholesale = await _context.ProductWholesalePrices
                .Where(wp => wp.ProductId == id && wp.IsActive
                    && wp.FixedPrice != null && wp.FixedPrice >= product.Price)
                .OrderByDescending(wp => wp.FixedPrice)
                .FirstOrDefaultAsync();

            if (conflictingWholesale != null)
            {
                throw new InvalidOperationException(
                    _localizer["The retail price ({0}) cannot be lower than or equal to an active wholesale price ({1})",
                        product.Price.ToString("N2"),
                        conflictingWholesale.FixedPrice!.Value.ToString("N2")]);
            }

            // Update audit fields
            product.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            product.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            return _mapper.Map<ProductDto>(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(long id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return false;
            }

            // Check if product has related records
            var hasRelatedRecords = await _context.SaleDetails
                .AnyAsync(x => x.ProductId == id);

            if (hasRelatedRecords)
            {
                throw new InvalidOperationException(
                    _localizer["Cannot delete product because it has related records"]);
            }

            product.DeletedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            throw;
        }
    }

    public async Task<ProductImageDto> AddProductImageAsync(
        long productId,
        Stream imageStream,
        string fileName,
        string contentType,
        bool isPrimary = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, cancellationToken);
            byte[] imageData = memoryStream.ToArray();

            // Crear thumbnail
            var (thumbnailData, savedContentType) = await _imageService.CreateThumbnailAsync(
                new MemoryStream(imageData),
                fileName,
                contentType,
                cancellationToken: cancellationToken);

            var image = new ProductImage
            {
                ProductId = productId,
                FileName = string.Empty,
                ThumbnailFileName = string.Empty,
                ImageData = imageData,
                ThumnailImageData = thumbnailData,
                ContentType = savedContentType,
                IsPrimary = isPrimary,
                CreatedBy = await _currentUserService.GetFullNameAsync() ?? throw new InvalidOperationException("Unknown user"),
                CreatedAt = _dateTime.Now
            };

            // Check if the primary image product already exists
            var existingImage = await _context.ProductImages
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsPrimary, cancellationToken);

            if (existingImage != null)
            {
                existingImage.ImageData = imageData;
                existingImage.ThumnailImageData = thumbnailData;
                existingImage.ContentType = savedContentType;
                existingImage.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? throw new InvalidOperationException("Unknown user");
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _context.ProductImages.Add(image);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return _mapper.Map<ProductImageDto>(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding image to product {ProductId}", productId);
            throw;
        }
    }

    public async Task<IList<ProductImageDto>> GetProductImagesAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Primero obtenemos los registros sin los datos binarios
            var images = await _context.ProductImages
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new ProductImage
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    ThumbnailFileName = x.ThumbnailFileName,
                    ContentType = x.ContentType,
                    IsPrimary = x.IsPrimary
                })
                .ToListAsync(cancellationToken);

            var result = new List<ProductImageDto>();

            foreach (var image in images)
            {
                result.Add(_mapper.Map<ProductImageDto>(image));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<bool> RemoveProductImageAsync(
        long productId,
        long imageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var image = await _context.ProductImages
                .FirstOrDefaultAsync(x =>
                    x.Id == imageId &&
                    x.ProductId == productId,
                    cancellationToken);

            if (image == null)
                return false;

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing image {ImageId} from product {ProductId}",
                imageId, productId);
            throw;
        }
    }

    public async Task<bool> SetPrimaryImageAsync(
        long productId,
        long imageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var images = await _context.ProductImages
                .Where(x => x.ProductId == productId)
                .ToListAsync(cancellationToken);

            var newPrimaryImage = images.FirstOrDefault(x => x.Id == imageId);
            if (newPrimaryImage == null)
                return false;

            foreach (var image in images)
            {
                image.IsPrimary = image.Id == imageId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary image {ImageId} for product {ProductId}",
                imageId, productId);
            throw;
        }
    }

    public async Task<bool> ValidateUniqueCodeAsync(string code, long? excludeId = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.Products.AsNoTracking();

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(x => x.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product code uniqueness");
            throw;
        }
    }

    public async Task<bool> ValidateUniqueBarcodeAsync(string barcode, long? excludeId = null)
    {
        if (string.IsNullOrEmpty(barcode))
            return true;

        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.Products.AsNoTracking();

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(x => x.Barcode == barcode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product barcode uniqueness");
            throw;
        }
    }


    public async Task<Result<List<ProductBulkLoadResultDto>>> CreateBulkProductsAsync(
        BulkProductLoadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProductBulkLoadResultDto>();

        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Get company settings to check if Mexico
            var settings = await _settingsService.GetSettingsAsync();
            bool isMexico = settings?.CountryCode == CountryCodes.Mexico;

            // Pre-load unit measures for validation
            var unitMeasures = await _context.UnitMeasures
                .AsNoTracking()
                .Where(x => x.CountryCode == settings!.CountryCode)
                .ToListAsync(cancellationToken);

            // Pre-load active wholesale tiers for bulk import
            var wholesaleTiers = await _context.WholesaleTiers
                .AsNoTracking()
                .Where(t => t.IsActive && t.IsDeleted == 0)
                .ToListAsync(cancellationToken);
            var tiersByName = wholesaleTiers.ToDictionary(t => t.Name, t => t.Id, StringComparer.OrdinalIgnoreCase);

            // Pre-load existing codes for validation
            var existingCodes = await _context.Products
                .AsNoTracking()
                .Select(p => p.Code.ToUpper())
                .ToHashSetAsync(cancellationToken);

            var existingBarcodes = await _context.Products
                .AsNoTracking()
                .Where(p => p.Barcode != null)
                .Select(p => p.Barcode!.ToUpper())
                .ToHashSetAsync(cancellationToken);

            // Track codes generated during this batch to avoid duplicates within the same import
            var generatedCodesInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Process each item
            foreach (var item in request.Items)
            {
                var result = new ProductBulkLoadResultDto
                {
                    ProductCode = item.Code ?? "AUTO-GENERATED",
                    ProductName = item.Name,
                    Brand = item.Brand,
                    Price = item.Price
                };

                try
                {
                    // Validate required fields
                    var validationResult = ValidateProductBulkItem(
                        item, isMexico, unitMeasures, existingCodes, existingBarcodes, generatedCodesInBatch);

                    if (!validationResult.IsSuccess)
                    {
                        result.Success = false;
                        result.Error = validationResult.Error;
                        results.Add(result);
                        continue;
                    }

                    // Generate code if empty
                    if (string.IsNullOrWhiteSpace(item.Code))
                    {
                        item.Code = await GenerateUniqueCodeForBatchAsync(
                            existingCodes, generatedCodesInBatch, cancellationToken);
                        result.ProductCode = item.Code;

                        // Add to batch tracking to prevent duplicates within this import
                        generatedCodesInBatch.Add(item.Code.ToUpper());
                    }
                    else
                    {
                        // Add manually provided code to batch tracking
                        generatedCodesInBatch.Add(item.Code.ToUpper());
                    }

                    // Find unit measure
                    var unitMeasure = unitMeasures.FirstOrDefault(um =>
                        um.Code.Equals(item.UnitMeasureCode, StringComparison.OrdinalIgnoreCase));

                    if (unitMeasure == null)
                    {
                        result.Success = false;
                        result.Error = _localizer["Unit measure '{0}' not found", item.UnitMeasureCode];
                        results.Add(result);
                        continue;
                    }

                    // Find Mexico product service if applicable
                    int? mexicoProductServiceId = null;
                    if (isMexico && !string.IsNullOrWhiteSpace(item.MexicoProductServiceCode))
                    {
                        var productService = await _context.MexicoProductServices
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ps => ps.Code == item.MexicoProductServiceCode,
                                cancellationToken);

                        if (productService == null)
                        {
                            result.Success = false;
                            result.Error = _localizer["SAT product service code '{0}' not found", item.MexicoProductServiceCode];
                            results.Add(result);
                            continue;
                        }
                        mexicoProductServiceId = productService.Id;
                    }

                    // Create product entity
                    var product = new Product
                    {
                        Code = item.Code,
                        Name = item.Name,
                        Brand = item.Brand,
                        Description = item.Description,
                        Barcode = string.IsNullOrWhiteSpace(item.Barcode) ? null : item.Barcode,
                        Content = item.Content,
                        UnitMeasureId = unitMeasure.Id,
                        Cost = item.Cost,
                        Price = item.Price,
                        IsTaxable = item.IsTaxable,
                        IsActive = item.IsActive,
                        MexicoProductServiceId = mexicoProductServiceId,
                        IsPartialSaleAllowed = item.AllowPartialSale,
                        QuantityStep = item.QuantityStep,
                        IsLabelingAllowed = item.AllowLabeling,
                        AllowCustomPricing = item.AllowCustomPricing,
                        CreatedBy = await _currentUserService.GetFullNameAsync() ?? "System",
                        CreatedAt = _dateTime.Now
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Create wholesale prices if provided in the import data
                    if (item.WholesalePrices.Count > 0)
                    {
                        foreach (var (tierName, wholesaleData) in item.WholesalePrices)
                        {
                            bool hasValue = wholesaleData.FixedPrice.HasValue
                                ? wholesaleData.MinQuantity > 0 && wholesaleData.FixedPrice.Value > 0
                                : wholesaleData.MinQuantity > 0 && wholesaleData.DiscountPercentage > 0;

                            if (tiersByName.TryGetValue(tierName, out var tierId) && hasValue)
                            {
                                var wholesalePrice = new ProductWholesalePrice
                                {
                                    ProductId = product.Id,
                                    WholesaleTierId = tierId,
                                    MinQuantity = wholesaleData.MinQuantity,
                                    DiscountPercentage = wholesaleData.DiscountPercentage,
                                    FixedPrice = wholesaleData.FixedPrice,
                                    IsActive = true,
                                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "System",
                                    CreatedAt = _dateTime.Now
                                };
                                _context.ProductWholesalePrices.Add(wholesalePrice);
                            }
                        }
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    // Update validation sets for next iterations
                    existingCodes.Add(item.Code.ToUpper());
                    if (!string.IsNullOrWhiteSpace(item.Barcode))
                    {
                        existingBarcodes.Add(item.Barcode.ToUpper());
                    }

                    result.Success = true;
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing product {Code}", item.Code ?? item.Name);
                    result.Success = false;
                    result.Error = _localizer["Error processing product: {0}", request.Items.IndexOf(item) + 1];
                    results.Add(result);
                }
            }

            // Save all valid products
            var successCount = results.Count(r => r.Success);
            if (successCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully created {Count} products in bulk operation", successCount);
            }

            return Result<List<ProductBulkLoadResultDto>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk product creation");
            return Result<List<ProductBulkLoadResultDto>>.Failure(
                _localizer["Error processing bulk product creation: {0}", ex.Message]);
        }
    }

    public async Task<Result<byte[]>> GetProductImageBinaryDataAsync(
        long imageId, 
        bool isThumbnail = false, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var image = await _context.ProductImages
                .AsNoTracking()
                .Where(x => x.Id == imageId)
                .Select(x => new 
                { 
                    x.ImageData, 
                    x.ThumnailImageData,
                    x.FileName,
                    x.ThumbnailFileName
                })
                .FirstOrDefaultAsync(cancellationToken);
                
            if (image == null)
            {
                return Result<byte[]>.Failure("Image not found");
            }
            
            var imageData = isThumbnail ? image.ThumnailImageData : image.ImageData;
            
            if (imageData == null || imageData.Length == 0)
            {
                return Result<byte[]>.Failure(
                    isThumbnail ? "Thumbnail data not available" : "Image data not available");
            }
            
            return Result<byte[]>.Success(imageData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting binary data for image {ImageId}", imageId);
            return Result<byte[]>.Failure("Error retrieving image data");
        }
    }

    public async Task<ProductImageDto?> GetProductImageByIdAsync(
        long imageId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var image = await _context.ProductImages
                .AsNoTracking()
                .Where(x => x.Id == imageId)
                .FirstOrDefaultAsync(cancellationToken);
                
            return image != null ? _mapper.Map<ProductImageDto>(image) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product image {ImageId}", imageId);
            throw;
        }
    }


    private Result<bool> ValidateProductBulkItem(
        ProductBulkLoadDto item,
        bool isMexico,
        List<UnitMeasure> unitMeasures,
        HashSet<string> existingCodes,
        HashSet<string> existingBarcodes,
        HashSet<string> generatedCodesInBatch)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(item.Name))
            return Result<bool>.Failure(_localizer["Product name is required"]);

        if (string.IsNullOrWhiteSpace(item.Brand))
            return Result<bool>.Failure(_localizer["Product brand is required"]);

        if (string.IsNullOrWhiteSpace(item.UnitMeasureCode))
            return Result<bool>.Failure(_localizer["Unit measure code is required"]);

        if (item.Content <= 0)
            return Result<bool>.Failure(_localizer["Content must be greater than 0"]);

        if (item.Price <= 0)
            return Result<bool>.Failure(_localizer["Price must be greater than 0"]);

        // Validate code uniqueness if provided
        if (!string.IsNullOrWhiteSpace(item.Code))
        {
            if (existingCodes.Contains(item.Code.ToUpper()) ||
                generatedCodesInBatch.Contains(item.Code.ToUpper()))
            {
                return Result<bool>.Failure(
                    _localizer["Product code '{0}' already exists or was already used in this import", item.Code]);
            }
        }

        // Validate barcode uniqueness if provided
        if (!string.IsNullOrWhiteSpace(item.Barcode) &&
            existingBarcodes.Contains(item.Barcode.ToUpper()))
        {
            return Result<bool>.Failure(
                _localizer["Product barcode '{0}' already exists", item.Barcode]);
        }

        // Validate unit measure exists
        if (!unitMeasures.Any(um => um.Code.Equals(item.UnitMeasureCode, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<bool>.Failure(
                _localizer["Unit measure '{0}' not found", item.UnitMeasureCode]);
        }

        // Validate Mexico-specific requirements
        if (isMexico && string.IsNullOrWhiteSpace(item.MexicoProductServiceCode))
        {
            return Result<bool>.Failure(
                _localizer["SAT product service code is required for Mexico"]);
        }

        return Result<bool>.Success(true);
    }


    /// <summary>
    /// Generates a unique product code that doesn't conflict with existing codes or codes generated in the current batch
    /// </summary>
    private async Task<string> GenerateUniqueCodeForBatchAsync(
        HashSet<string> existingCodes,
        HashSet<string> generatedCodesInBatch,
        CancellationToken cancellationToken,
        int maxAttempts = 100)
    {
        string generatedCode;
        int attempts = 0;

        do
        {
            generatedCode = await _codeGeneratorService.GenerateProductCodeAsync(cancellationToken);
            attempts++;

            // Check if code is unique against both existing codes and codes generated in this batch
            bool isUnique = !existingCodes.Contains(generatedCode.ToUpper()) &&
                           !generatedCodesInBatch.Contains(generatedCode.ToUpper());

            if (isUnique)
            {
                _logger.LogDebug("Generated unique code {Code} on attempt {Attempt}", generatedCode, attempts);
                return generatedCode;
            }

            _logger.LogDebug("Generated code {Code} conflicts with existing data on attempt {Attempt}",
                generatedCode, attempts);

        } while (attempts < maxAttempts);

        // If we couldn't generate a unique code after max attempts, throw an exception
        throw new InvalidOperationException(
            _localizer["Unable to generate unique product code after {0} attempts. " +
                     "Please review the product code generation configuration.", maxAttempts]);
    }

    public async Task<byte[]> ExportCatalogAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Use AsNoTracking for performance since we're only reading data
        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.UnitMeasure)
            .Where(p => p.IsDeleted == 0)
            .OrderBy(p => p.Code)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Brand = p.Brand,
                Description = p.Description,
                Barcode = p.Barcode,
                Content = p.Content,
                UnitMeasureName = p.UnitMeasure!.Name,
                Cost = p.Cost,
                Price = p.Price,
                IsTaxable = p.IsTaxable,
                IsActive = p.IsActive,
                IsPartialSaleAllowed = p.IsPartialSaleAllowed,
                QuantityStep = p.QuantityStep,
                IsLabelingAllowed = p.IsLabelingAllowed,
                AllowCustomPricing = p.AllowCustomPricing
            })
            .ToListAsync();

        // Get active fractions for column headers
        var fractions = await context.PartialSaleFractions
            .AsNoTracking()
            .Where(f => f.IsActive && f.IsDeleted == 0)
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new FractionColumnDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                DisplayOrder = f.DisplayOrder
            })
            .ToListAsync();

        // Get partial sale surcharges for products that allow partial sales
        var productIds = products
            .Where(p => p.IsPartialSaleAllowed)
            .Select(p => p.Id)
            .ToList();

        var surcharges = new List<ProductSurchargeExportDto>();
        if (productIds.Count > 0)
        {
            surcharges = await context.ProductPartialSurcharges
                .AsNoTracking()
                .Include(s => s.PartialSaleFraction)
                .Where(s => productIds.Contains(s.ProductId) && s.IsDeleted == 0 && s.IsActive)
                .Select(s => new ProductSurchargeExportDto
                {
                    ProductId = s.ProductId,
                    FractionCode = s.PartialSaleFraction.Code,
                    SurchargePercentage = s.SurchargePercentage,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        // Get active wholesale tiers for column headers
        var wholesaleTiers = await context.WholesaleTiers
            .AsNoTracking()
            .Where(t => t.IsActive && t.IsDeleted == 0)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new WholesaleTierColumnDto
            {
                Id = t.Id,
                Name = t.Name,
                DisplayOrder = t.DisplayOrder
            })
            .ToListAsync();

        // Get wholesale prices for all products
        var allProductIds = products.Select(p => p.Id).ToList();
        var wholesalePrices = new List<ProductWholesaleExportDto>();
        if (allProductIds.Count > 0 && wholesaleTiers.Count > 0)
        {
            wholesalePrices = await context.ProductWholesalePrices
                .AsNoTracking()
                .Include(wp => wp.WholesaleTier)
                .Where(wp => allProductIds.Contains(wp.ProductId) && wp.IsDeleted == 0 && wp.IsActive)
                .Select(wp => new ProductWholesaleExportDto
                {
                    ProductId = wp.ProductId,
                    TierName = wp.WholesaleTier.Name,
                    MinQuantity = wp.MinQuantity,
                    DiscountPercentage = wp.DiscountPercentage,
                    FixedPrice = wp.FixedPrice,
                    IsActive = wp.IsActive
                })
                .ToListAsync();
        }

        // Use the ExcelExportService for consistent formatting and localization
        var excelService = _serviceProvider.GetRequiredService<IExcelExportService>();
        var culture = CultureInfo.CurrentCulture;

        return await excelService.ExportProductCatalogToExcelAsync(
            products, culture, fractions, surcharges, wholesaleTiers, wholesalePrices);
    }
}