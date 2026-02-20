using AutoMapper;

using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Shop;
// using App.Core.DTOs.Warehouse; // TODO: Update for Location-based sales
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class SaleService : ISaleService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<SaleService> _logger;
    private readonly IStringLocalizer<SaleService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IDiscountSettingsService _discountSettingsService;
    private readonly IDiscountAuthorizerService _discountAuthorizerService;
    private readonly IInventoryService _inventoryService;
    private readonly ITaxRateService _taxRateService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ITaxSettingsService _taxSettingsService;
    private readonly IProductPartialSurchargeService _productPartialSurchargeService;
    private readonly IRoundingSettingsService _roundingSettingsService;

    public SaleService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<SaleService> logger,
        IStringLocalizer<SaleService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IDiscountSettingsService discountSettingsService,
        IDiscountAuthorizerService discountAuthorizerService,
        IInventoryService inventoryService,
        ITaxRateService taxRateService,
        ICompanySettingsService companySettingsService,
        ITaxSettingsService taxSettingsService,
        IProductPartialSurchargeService productPartialSurchargeService,
        IRoundingSettingsService roundingSettingsService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _discountSettingsService = discountSettingsService;
        _discountAuthorizerService = discountAuthorizerService;
        _inventoryService = inventoryService;
        _taxRateService = taxRateService;
        _companySettingsService = companySettingsService;
        _taxSettingsService = taxSettingsService;
        _productPartialSurchargeService = productPartialSurchargeService;
        _roundingSettingsService = roundingSettingsService;
    }

    public async Task<(int TotalCount, IList<SaleDto> Items)> GetSalesAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        long? customerId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null,
        SaleType? saleType = null,
        int? locationId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Sale> query = context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Location)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(s =>
                    s.Customer.Name.Contains(searchString));
            }

            if (customerId.HasValue)
            {
                query = query.Where(s => s.CustomerId == customerId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var nextDay = endDate.Value.AddDays(1);
                query = query.Where(s => s.SaleDate < nextDay);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<App.Core.Enums.Shop.SaleStatus>(status, out var statusEnum))
            {
                query = query.Where(s => s.Status == statusEnum);
            }

            if (saleType.HasValue)
            {
                query = query.Where(s => s.SaleType == saleType.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(s => s.LocationId == locationId.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var salesDtos = items.Select(s => _mapper.Map<SaleDto>(s)).ToList();

            return (totalCount, salesDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales");
            throw;
        }
    }

    public async Task<SaleDto?> GetSaleByIdAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var sale = await context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Location)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            return sale != null ? _mapper.Map<SaleDto>(sale) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale by ID {Id}", id);
            throw;
        }
    }

    public async Task<Result<SaleDto>> CreateSaleAsync(CreateSaleDto createDto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Validate system configurations first
            var configValidation = await ValidateSystemConfigurationsAsync();
            if (!configValidation.IsSuccess)
            {
                return Result<SaleDto>.Failure(configValidation.Error!);
            }

            // Validate sale data
            var validationResult = await ValidateSaleDataAsync(createDto);
            if (!validationResult.IsSuccess)
            {
                return Result<SaleDto>.Failure(validationResult.Error!);
            }

            // TODO: Implement Location-based warehouse logic
            // For now, using LocationId from DTO - needs proper validation
            int? locationId = createDto.LocationId;

            // Validate discount
            var discountResult = await ValidateDiscountAsync(
                createDto.DiscountPercentage,
                createDto.DiscountAuthorizerId);

            if (!discountResult.IsSuccess)
            {
                return Result<SaleDto>.Failure(discountResult.Error!);
            }

            // Calculate sale
            var saleCalculation = await CalculateSaleAsync(context, createDto, locationId);
            if (!saleCalculation.IsSuccess)
            {
                return Result<SaleDto>.Failure(saleCalculation.Error!);
            }

            var (sale, detailsToProcess) = saleCalculation.Value!;

            // Save the sale entity
            context.Sales.Add(sale);
            await context.SaveChangesAsync();

            // Process inventory for each detail
            foreach (var detail in detailsToProcess)
            {
                var movementResult = await _inventoryService.CreateMovementAsync(new CreateInventoryMovementDto
                {
                    ProductId = detail.ProductId,
                    LocationId = locationId ?? 0, // TODO: Validate locationId properly
                    Quantity = detail.Quantity,
                    MovementType = InventoryMovementType.Sale,
                    MovementSubType = InventoryMovementSubType.DirectSale,
                    Reference = $"Sale-{sale.Id}",
                    Reason = $"Sale of {detail.Quantity} units"
                });

                if (!movementResult.Success)
                {
                    throw new InvalidOperationException(
                        L["Error processing inventory: {0}", movementResult.Message ?? "Unknown error"]);
                }
            }

            await transaction.CommitAsync();

            return Result<SaleDto>.Success(_mapper.Map<SaleDto>(sale));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating sale");
            return Result<SaleDto>.Failure(L["An error occurred while creating the sale: {0}", ex.Message]);
        }
    }

    public async Task<Result<SaleDto>> UpdateSaleAsync(long id, UpdateSaleDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var sale = await context.Sales
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return Result<SaleDto>.Failure(L["Sale not found with ID: {0}", id]);
            }

            if (sale.Status == App.Core.Enums.Shop.SaleStatus.Cancelled)
            {
                return Result<SaleDto>.Failure(L["Cannot update a cancelled sale"]);
            }

            // Validate status change
            if (updateDto.Status != sale.Status)
            {
                var statusChangeResult = ValidateSaleStatusChange(sale.Status, updateDto.Status);
                if (!statusChangeResult.IsSuccess)
                {
                    return Result<SaleDto>.Failure(statusChangeResult.Error!);
                }
            }

            // Validate discount if changed
            if (updateDto.DiscountPercentage != sale.DiscountPercentage)
            {
                var discountResult = await ValidateDiscountAsync(
                    updateDto.DiscountPercentage,
                    updateDto.DiscountAuthorizedBy);

                if (!discountResult.IsSuccess)
                {
                    return Result<SaleDto>.Failure(discountResult.Error!);
                }
            }

            // Update properties
            sale.Status = updateDto.Status;
            sale.PaymentMethod = updateDto.PaymentMethod;

            // Update discount if changed and recalculate totals
            if (updateDto.DiscountPercentage != sale.DiscountPercentage)
            {
                sale.DiscountPercentage = updateDto.DiscountPercentage;
                sale.DiscountAuthorizedBy = updateDto.DiscountAuthorizedBy;

                // Recalculate discount amount and total
                sale.DiscountAmount = sale.Subtotal * (sale.DiscountPercentage / 100);
                sale.Total = sale.Subtotal - sale.DiscountAmount + sale.TaxAmount;
            }

            // Update audit fields
            sale.ModifiedBy = _currentUserService.FullName;
            sale.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return Result<SaleDto>.Success(_mapper.Map<SaleDto>(sale));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sale {Id}", id);
            return Result<SaleDto>.Failure(L["An error occurred while updating the sale: {0}", ex.Message]);
        }
    }

    public async Task<Result<bool>> CancelSaleAsync(long id, string reason)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var sale = await context.Sales
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return Result<bool>.Failure(L["Sale not found with ID: {0}", id]);
            }

            if (sale.Status == App.Core.Enums.Shop.SaleStatus.Cancelled)
            {
                return Result<bool>.Success(true); // Already cancelled
            }

            // Use LocationId from the sale entity
            int? locationId = sale.LocationId;

            // Return inventory
            foreach (var detail in sale.Details)
            {
                var movementResult = await _inventoryService.CreateMovementAsync(new CreateInventoryMovementDto
                {
                    ProductId = detail.ProductId,
                    LocationId = locationId ?? 0, // TODO: Validate locationId properly
                    Quantity = detail.Quantity,
                    MovementType = InventoryMovementType.Return,
                    MovementSubType = InventoryMovementSubType.DirectSale,
                    Reference = $"Cancel-Sale-{sale.Id}",
                    Reason = reason
                });

                if (!movementResult.Success)
                {
                    throw new InvalidOperationException(
                        L["Error returning inventory: {0}", movementResult.Message ?? "Unknown error"]);
                }
            }

            // Update sale status
            sale.Status = App.Core.Enums.Shop.SaleStatus.Cancelled;
            sale.ModifiedBy = _currentUserService.FullName;
            sale.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error cancelling sale {Id}", id);
            return Result<bool>.Failure(L["An error occurred while cancelling the sale: {0}", ex.Message]);
        }
    }

    public async Task<Result<bool>> ValidateDiscountAsync(
        decimal discountPercentage,
        string? authorizerId = null)
    {
        try
        {
            // Get discount settings
            var discountSettingsResult = await _discountSettingsService.GetSettingsAsync();
            if (!discountSettingsResult.IsSuccess)
            {
                return Result<bool>.Failure(L["Error retrieving discount settings"]);
            }

            var settings = discountSettingsResult.Value!;

            // Verify that the authorizer can authorize discounts
            if (discountPercentage > 0 && !string.IsNullOrEmpty(authorizerId))
            {
                var canAuthorize = await _discountAuthorizerService.CanUserAuthorizeDiscountsAsync(authorizerId);
                if (!canAuthorize)
                {
                    return Result<bool>.Failure(L["The specified user is not authorized to approve discounts"]);
                }
            }

            // Validate public sale discount
            if (discountPercentage > settings.MaximumPublicDiscount)
            {
                return Result<bool>.Failure(
                    L["Public discount cannot exceed {0}%", settings.MaximumPublicDiscount]);
            }

            if (settings.RequireAuthorizationForPublicDiscount && discountPercentage > 0 && string.IsNullOrEmpty(authorizerId))
            {
                return Result<bool>.Failure(
                    L["Authorization required for any public discount"]);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating discount");
            return Result<bool>.Failure(L["An error occurred while validating the discount"]);
        }
    }

    // Private helper methods

    private async Task<Result> ValidateSaleDataAsync(CreateSaleDto createDto)
    {
        if (createDto.Details == null || !createDto.Details.Any())
        {
            return Result.Failure(L["Sale must have at least one product"]);
        }

        // Validate customer exists
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customerExists = await context.Customers.AnyAsync(c => c.Id == createDto.CustomerId);
        if (!customerExists)
        {
            return Result.Failure(L["Customer not found with ID: {0}", createDto.CustomerId]);
        }

        return Result.Success();
    }

    // TODO: Reimplement this method using Location instead of Branch/Warehouse
    // private async Task<Result<(Warehouse Warehouse, int Id)>> GetWarehouseForSaleAsync()
    // {
    //     await using var context = await _contextFactory.CreateDbContextAsync();
    //
    //     // Get warehouse from active branch
    //     var activeBranchId = _currentUserService.ActiveBranchId;
    //     if (!activeBranchId.HasValue)
    //     {
    //         return Result<(Warehouse, int)>.Failure(L["No active branch selected. Please select a branch to continue."]);
    //     }
    //
    //     var warehouse = await context.Warehouses
    //         .FirstOrDefaultAsync(w => w.BranchId == activeBranchId.Value && w.IsActive);
    //
    //     if (warehouse == null)
    //     {
    //         return Result<(Warehouse, int)>.Failure(L["No active warehouse found for the selected branch"]);
    //     }
    //
    //     return Result<(Warehouse, int)>.Success((warehouse, warehouse.Id));
    // }

    private Result ValidateSaleStatusChange(App.Core.Enums.Shop.SaleStatus currentStatus, App.Core.Enums.Shop.SaleStatus newStatus)
    {
        // Define valid status transitions - only allow Created -> Cancelled
        var validTransitions = new Dictionary<App.Core.Enums.Shop.SaleStatus, App.Core.Enums.Shop.SaleStatus[]>
        {
            { App.Core.Enums.Shop.SaleStatus.Created, new[] { App.Core.Enums.Shop.SaleStatus.Cancelled } }
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowedStatuses) ||
            !allowedStatuses.Contains(newStatus))
        {
            return Result.Failure(L["Invalid status transition from {0} to {1}", currentStatus, newStatus]);
        }

        return Result.Success();
    }

    private async Task<Result<(Sale Sale, List<SaleDetail> Details)>> CalculateSaleAsync(
        ApplicationDbContext context,
        CreateSaleDto createDto,
        int? locationId)
    {
        try
        {
            // Validate locationId is provided
            if (!locationId.HasValue)
            {
                return Result<(Sale, List<SaleDetail>)>.Failure(
                    L["Location is required for sales"]);
            }

            // Create new sale entity
            var sale = new Sale
            {
                CustomerId = createDto.CustomerId,
                SaleDate = createDto.SaleDate ?? _dateTime.Now,
                PaymentMethod = createDto.PaymentMethod,
                Status = App.Core.Enums.Shop.SaleStatus.Created,
                SaleType = createDto.SaleType,
                LocationId = createDto.LocationId,
                DiscountPercentage = createDto.DiscountPercentage,
                DiscountAuthorizedBy = createDto.DiscountAuthorizedBy,
                CreatedBy = _currentUserService.FullName,
                CreatedAt = _dateTime.Now,
                Details = new List<SaleDetail>()
            };

            // Fetch all products in a single query
            var productIds = createDto.Details.Select(d => d.ProductId).ToList();
            var productsQuery = await context.Products
                .AsNoTracking()
                .Include(p => p.UnitMeasure)
                .Where(p => p.IsActive)
                .ToListAsync();

            var products = productsQuery
                .Where(p => productIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p);

            // Verify all products exist
            var missingProductIds = productIds.Except(products.Keys).ToList();
            if (missingProductIds.Any())
            {
                return Result<(Sale, List<SaleDetail>)>.Failure(
                    L["One or more products not found: {0}", string.Join(", ", missingProductIds)]);
            }

            // Check stock availability for all products
            var insufficientStockProducts = new List<string>();
            foreach (var detailDto in createDto.Details)
            {
                var stockAvailable = await _inventoryService.ValidateStockAvailabilityAsync(
                    detailDto.ProductId, locationId.Value, detailDto.Quantity);

                if (!stockAvailable)
                {
                    var product = products[detailDto.ProductId];
                    insufficientStockProducts.Add(product.Name);
                }
            }

            if (insufficientStockProducts.Any())
            {
                return Result<(Sale, List<SaleDetail>)>.Failure(
                    L["Insufficient stock for products: {0}",
                    string.Join(", ", insufficientStockProducts)]);
            }

            // Get tax rate
            decimal defaultTaxRate = await _taxRateService.GetEffectiveRateAsync("MX"); // Assuming Mexico for now

            // Calculate all details and totals
            decimal subtotal = 0;
            decimal taxAmount = 0;
            decimal discountAmount = 0;
            var detailsToProcess = new List<SaleDetail>();

            foreach (var detailDto in createDto.Details)
            {
                var product = products[detailDto.ProductId];
                decimal taxRate = product.IsTaxable ? defaultTaxRate : 0;

                // Calculate unit price and subtotal based on partial sales capability
                decimal effectiveUnitPrice;
                decimal detailSubtotal;
                decimal surchargePercentage = 0;
                decimal surchargeAmount = 0;
                decimal basePriceBeforeSurcharge = 0;
                int? partialSaleFractionId = null;

                if (product.IsPartialSaleAllowed && product.Content > 0)
                {
                    // For partial sales, calculate price with potential surcharge
                    var fractionalPriceResult = await _productPartialSurchargeService
                        .CalculateFractionalPriceAsync(
                            product.Id,
                            detailDto.Quantity,
                            product.Content,
                            product.Price,
                            detailDto.PartialSaleFractionId);

                    if (fractionalPriceResult.IsSuccess)
                    {
                        var calc = fractionalPriceResult.Value!;
                        effectiveUnitPrice = calc.Quantity > 0 ? calc.FinalPrice / calc.Quantity : 0;
                        detailSubtotal = calc.FinalPrice;
                        surchargePercentage = calc.SurchargePercentage;
                        surchargeAmount = calc.SurchargeAmount;
                        basePriceBeforeSurcharge = calc.BasePriceBeforeSurcharge;
                        partialSaleFractionId = calc.FractionId;
                    }
                    else
                    {
                        // Fallback to proportional pricing if calculation fails
                        effectiveUnitPrice = product.Price / product.Content;
                        detailSubtotal = effectiveUnitPrice * detailDto.Quantity;
                        basePriceBeforeSurcharge = detailSubtotal;
                    }
                }
                else
                {
                    // For regular sales, use the full product price
                    effectiveUnitPrice = product.Price;
                    detailSubtotal = product.Price * detailDto.Quantity;
                    basePriceBeforeSurcharge = detailSubtotal;
                }

                decimal detailDiscountAmount = detailSubtotal * (detailDto.DiscountPercentage / 100);
                decimal detailAfterDiscount = detailSubtotal - detailDiscountAmount;
                decimal detailTaxAmount = detailAfterDiscount * (taxRate / 100);
                decimal detailTotal = detailAfterDiscount + detailTaxAmount;

                // Create sale detail
                var detail = new SaleDetail
                {
                    ProductId = detailDto.ProductId,
                    Quantity = detailDto.Quantity,
                    UnitPrice = effectiveUnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    DiscountAmount = detailDiscountAmount,
                    TaxRate = taxRate,
                    TaxAmount = detailTaxAmount,
                    Subtotal = detailSubtotal,
                    Total = detailTotal,
                    PartialSaleFractionId = partialSaleFractionId,
                    SurchargePercentage = surchargePercentage,
                    SurchargeAmount = surchargeAmount,
                    BasePriceBeforeSurcharge = basePriceBeforeSurcharge,
                    CreatedBy = _currentUserService.FullName,
                    CreatedAt = _dateTime.Now
                };

                sale.Details.Add(detail);
                detailsToProcess.Add(detail);

                // Add to sale totals
                subtotal += detailSubtotal;
                taxAmount += detailTaxAmount;
                discountAmount += detailDiscountAmount;
            }

            // Additional sale discount (if any)
            decimal additionalDiscountAmount = 0;
            if (createDto.DiscountPercentage > 0)
            {
                additionalDiscountAmount = (subtotal - discountAmount) * (createDto.DiscountPercentage / 100);
                discountAmount += additionalDiscountAmount;
            }

            // Update sale totals
            sale.Subtotal = subtotal;
            sale.TaxAmount = taxAmount;
            sale.DiscountAmount = discountAmount;

            // Calculate pre-rounding total
            decimal preRoundingTotal = subtotal - discountAmount + taxAmount;

            // Apply rounding if enabled
            var roundingResult = await _roundingSettingsService.ApplyRoundingAsync(preRoundingTotal);
            if (roundingResult.IsSuccess)
            {
                sale.RoundingAmount = roundingResult.Value.RoundingAmount;
                sale.Total = roundingResult.Value.RoundedTotal;
            }
            else
            {
                // If rounding service fails, use unrounded total
                sale.RoundingAmount = 0;
                sale.Total = preRoundingTotal;
                _logger.LogWarning("Rounding calculation failed, using unrounded total");
            }

            return Result<(Sale, List<SaleDetail>)>.Success((sale, detailsToProcess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating sale");
            return Result<(Sale, List<SaleDetail>)>.Failure(
                L["Error calculating sale: {0}", ex.Message]);
        }
    }

    private async Task<Result> ValidateSystemConfigurationsAsync()
    {
        // Validate Company Settings
        var companySettings = await _companySettingsService.GetSettingsAsync();
        if (companySettings == null)
        {
            return Result.Failure(L["Company settings not configured. Please configure company settings before creating sales."]);
        }

        // Validate Tax Settings
        var taxSettings = await _taxSettingsService.GetSettingsAsync();
        if (taxSettings == null)
        {
            return Result.Failure(L["Tax settings not configured. Please configure tax settings before creating sales."]);
        }

        // Validate effective tax rate exists
        try
        {
            var taxRate = await _taxRateService.GetEffectiveRateAsync(companySettings.CountryCode);
            if (taxRate == 0)
            {
                return Result.Failure(L["No effective tax rate found. Please configure at least one active tax rate."]);
            }
        }
        catch (Exception)
        {
            return Result.Failure(L["Error getting tax rate. Please ensure tax rates are properly configured."]);
        }

        return Result.Success();
    }
}
