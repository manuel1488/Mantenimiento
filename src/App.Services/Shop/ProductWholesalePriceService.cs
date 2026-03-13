using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class ProductWholesalePriceService : IProductWholesalePriceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductWholesalePriceService> _logger;
    private readonly IStringLocalizer<ProductWholesalePriceService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public ProductWholesalePriceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ProductWholesalePriceService> logger,
        IStringLocalizer<ProductWholesalePriceService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Result<IList<ProductWholesalePriceDto>>> GetWholesalePricesForProductAsync(long productId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var wholesalePrices = await context.ProductWholesalePrices
                .AsNoTracking()
                .Include(wp => wp.WholesaleTier)
                .Where(wp => wp.ProductId == productId)
                .OrderBy(wp => wp.WholesaleTier.DisplayOrder)
                .ToListAsync();

            var dtos = wholesalePrices.Select(wp => new ProductWholesalePriceDto
            {
                Id = wp.Id,
                ProductId = wp.ProductId,
                WholesaleTierId = wp.WholesaleTierId,
                TierName = wp.WholesaleTier.Name,
                MinQuantity = wp.MinQuantity,
                DiscountPercentage = wp.DiscountPercentage,
                FixedPrice = wp.FixedPrice,
                IsActive = wp.IsActive
            }).ToList();

            return Result<IList<ProductWholesalePriceDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wholesale prices for product {ProductId}", productId);
            return Result<IList<ProductWholesalePriceDto>>.Failure(L["Error retrieving wholesale prices"]);
        }
    }

    public async Task<Result<IDictionary<long, IList<ProductWholesalePriceDto>>>> GetWholesalePricesForProductsAsync(IList<long> productIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entities = await context.ProductWholesalePrices
                .AsNoTracking()
                .Include(wp => wp.WholesaleTier)
                .Where(wp => productIds.Contains(wp.ProductId))
                .ToListAsync();

            var result = entities
                .GroupBy(wp => wp.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (IList<ProductWholesalePriceDto>)g
                        .OrderBy(wp => wp.WholesaleTier.DisplayOrder)
                        .Select(wp => new ProductWholesalePriceDto
                        {
                            Id = wp.Id,
                            ProductId = wp.ProductId,
                            WholesaleTierId = wp.WholesaleTierId,
                            TierName = wp.WholesaleTier.Name,
                            MinQuantity = wp.MinQuantity,
                            DiscountPercentage = wp.DiscountPercentage,
                            FixedPrice = wp.FixedPrice,
                            IsActive = wp.IsActive
                        }).ToList());

            return Result<IDictionary<long, IList<ProductWholesalePriceDto>>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wholesale prices for products batch");
            return Result<IDictionary<long, IList<ProductWholesalePriceDto>>>.Failure(L["Error retrieving wholesale prices"]);
        }
    }

    public async Task<Result<decimal>> GetDiscountPercentageAsync(long productId, int tierId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var wholesalePrice = await context.ProductWholesalePrices
                .AsNoTracking()
                .FirstOrDefaultAsync(wp =>
                    wp.ProductId == productId &&
                    wp.WholesaleTierId == tierId &&
                    wp.IsActive);

            // Return 0 if no discount is configured
            return Result<decimal>.Success(wholesalePrice?.DiscountPercentage ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discount for product {ProductId}, tier {TierId}", productId, tierId);
            return Result<decimal>.Failure(L["Error retrieving wholesale discount"]);
        }
    }

    public async Task<Result> UpdateProductWholesalePricesAsync(UpdateProductWholesalePricesDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            // Verify product exists
            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
                return Result.Failure(L["Product not found"]);

            // Get existing wholesale prices
            var existingPrices = await context.ProductWholesalePrices
                .Where(wp => wp.ProductId == dto.ProductId)
                .ToListAsync();

            // Process each wholesale price in the update
            foreach (var priceDto in dto.WholesalePrices)
            {
                var existing = existingPrices
                    .FirstOrDefault(wp => wp.WholesaleTierId == priceDto.WholesaleTierId);

                if (existing != null)
                {
                    // Update existing
                    existing.MinQuantity = priceDto.MinQuantity;
                    existing.DiscountPercentage = priceDto.DiscountPercentage;
                    existing.FixedPrice = priceDto.FixedPrice;
                    existing.IsActive = priceDto.IsActive;
                    existing.ModifiedBy = _currentUserService.FullName;
                    existing.ModifiedAt = _dateTime.Now;
                }
                else
                {
                    // Create new
                    var newPrice = new ProductWholesalePrice
                    {
                        ProductId = dto.ProductId,
                        WholesaleTierId = priceDto.WholesaleTierId,
                        MinQuantity = priceDto.MinQuantity,
                        DiscountPercentage = priceDto.DiscountPercentage,
                        FixedPrice = priceDto.FixedPrice,
                        IsActive = priceDto.IsActive,
                        CreatedBy = _currentUserService.FullName ?? "System",
                        CreatedAt = _dateTime.Now
                    };
                    context.ProductWholesalePrices.Add(newPrice);
                }
            }

            // Soft delete prices that are no longer in the list
            var tierIdsInUpdate = dto.WholesalePrices.Select(wp => wp.WholesaleTierId).ToHashSet();
            var pricesToRemove = existingPrices
                .Where(wp => !tierIdsInUpdate.Contains(wp.WholesaleTierId))
                .ToList();

            foreach (var toRemove in pricesToRemove)
            {
                toRemove.IsDeleted = 1;
                toRemove.DeletedBy = _currentUserService.FullName;
                toRemove.DeletedAt = _dateTime.Now;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wholesale prices for product {ProductId}", dto.ProductId);
            return Result.Failure(L["Error updating wholesale prices"]);
        }
    }
}
