using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class ProductPartialSurchargeService : IProductPartialSurchargeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductPartialSurchargeService> _logger;
    private readonly IStringLocalizer<ProductPartialSurchargeService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IPartialSaleFractionService _fractionService;

    public ProductPartialSurchargeService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ProductPartialSurchargeService> logger,
        IStringLocalizer<ProductPartialSurchargeService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IPartialSaleFractionService fractionService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _fractionService = fractionService;
    }

    public async Task<Result<IList<ProductPartialSurchargeDto>>> GetSurchargesForProductAsync(long productId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var surcharges = await context.ProductPartialSurcharges
                .AsNoTracking()
                .Include(s => s.PartialSaleFraction)
                .Where(s => s.ProductId == productId)
                .OrderBy(s => s.PartialSaleFraction.DisplayOrder)
                .ToListAsync();

            var dtos = surcharges.Select(s => new ProductPartialSurchargeDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                PartialSaleFractionId = s.PartialSaleFractionId,
                FractionCode = s.PartialSaleFraction.Code,
                FractionName = s.PartialSaleFraction.Name,
                FractionValue = s.PartialSaleFraction.FractionValue,
                SurchargePercentage = s.SurchargePercentage,
                IsActive = s.IsActive
            }).ToList();

            return Result<IList<ProductPartialSurchargeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surcharges for product {ProductId}", productId);
            return Result<IList<ProductPartialSurchargeDto>>.Failure(L["Error retrieving surcharges"]);
        }
    }

    public async Task<Result<decimal>> GetSurchargePercentageAsync(long productId, int fractionId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var surcharge = await context.ProductPartialSurcharges
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ProductId == productId &&
                    s.PartialSaleFractionId == fractionId &&
                    s.IsActive);

            // Return 0 if no surcharge is configured (proportional pricing)
            return Result<decimal>.Success(surcharge?.SurchargePercentage ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge for product {ProductId}, fraction {FractionId}", productId, fractionId);
            return Result<decimal>.Failure(L["Error retrieving surcharge"]);
        }
    }

    public async Task<Result> UpdateProductSurchargesAsync(UpdateProductPartialSurchargesDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                // Verify product exists and allows partial sales
                var product = await context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

                if (product == null)
                    return Result.Failure(L["Product not found"]);

                if (!product.IsPartialSaleAllowed)
                    return Result.Failure(L["Product does not allow partial sales"]);

                // Get existing surcharges
                var existingSurcharges = await context.ProductPartialSurcharges
                    .Where(s => s.ProductId == dto.ProductId)
                    .ToListAsync();

                // Process each surcharge in the update
                foreach (var surchargeDto in dto.Surcharges)
                {
                    var existing = existingSurcharges
                        .FirstOrDefault(s => s.PartialSaleFractionId == surchargeDto.PartialSaleFractionId);

                    if (existing != null)
                    {
                        // Update existing
                        existing.SurchargePercentage = surchargeDto.SurchargePercentage;
                        existing.IsActive = surchargeDto.IsActive;
                        existing.ModifiedBy = await _currentUserService.GetFullNameAsync();
                        existing.ModifiedAt = _dateTime.Now;
                    }
                    else
                    {
                        // Create new
                        var newSurcharge = new ProductPartialSurcharge
                        {
                            ProductId = dto.ProductId,
                            PartialSaleFractionId = surchargeDto.PartialSaleFractionId,
                            SurchargePercentage = surchargeDto.SurchargePercentage,
                            IsActive = surchargeDto.IsActive,
                            CreatedBy = await _currentUserService.GetFullNameAsync() ?? "System",
                            CreatedAt = _dateTime.Now
                        };
                        context.ProductPartialSurcharges.Add(newSurcharge);
                    }
                }

                // Soft delete surcharges that are no longer in the list
                var fractionIdsInUpdate = dto.Surcharges.Select(s => s.PartialSaleFractionId).ToHashSet();
                var surchargesToRemove = existingSurcharges
                    .Where(s => !fractionIdsInUpdate.Contains(s.PartialSaleFractionId))
                    .ToList();

                foreach (var toRemove in surchargesToRemove)
                {
                    toRemove.IsDeleted = 1;
                    toRemove.DeletedBy = await _currentUserService.GetFullNameAsync();
                    toRemove.DeletedAt = _dateTime.Now;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result.Success();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating surcharges for product {ProductId}", dto.ProductId);
            return Result.Failure(L["Error updating surcharges"]);
        }
    }

    public async Task<Result<FractionalPriceCalculationDto>> CalculateFractionalPriceAsync(
        long productId,
        decimal quantity,
        decimal productContent,
        decimal productPrice,
        int? fractionId = null)
    {
        try
        {
            // Calculate base unit price (e.g., $100/19L = $5.26/L)
            decimal baseUnitPrice = productContent > 0 ? productPrice / productContent : productPrice;

            // Calculate base price for the quantity (before surcharge)
            decimal basePriceBeforeSurcharge = baseUnitPrice * quantity;

            // Get surcharge percentage if a fraction is selected
            decimal surchargePercentage = 0;
            string? fractionCode = null;

            if (fractionId.HasValue)
            {
                var surchargeResult = await GetSurchargePercentageAsync(productId, fractionId.Value);
                if (surchargeResult.IsSuccess)
                    surchargePercentage = surchargeResult.Value;

                var fractionResult = await _fractionService.GetFractionByIdAsync(fractionId.Value);
                if (fractionResult.IsSuccess)
                    fractionCode = fractionResult.Value?.Code;
            }

            // Calculate surcharge amount and final price
            decimal surchargeAmount = basePriceBeforeSurcharge * (surchargePercentage / 100);
            decimal finalPrice = basePriceBeforeSurcharge + surchargeAmount;

            return Result<FractionalPriceCalculationDto>.Success(new FractionalPriceCalculationDto
            {
                ProductId = productId,
                BaseUnitPrice = baseUnitPrice,
                Quantity = quantity,
                FractionId = fractionId,
                FractionCode = fractionCode,
                SurchargePercentage = surchargePercentage,
                BasePriceBeforeSurcharge = basePriceBeforeSurcharge,
                SurchargeAmount = surchargeAmount,
                FinalPrice = finalPrice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fractional price for product {ProductId}", productId);
            return Result<FractionalPriceCalculationDto>.Failure(L["Error calculating fractional price"]);
        }
    }
}
