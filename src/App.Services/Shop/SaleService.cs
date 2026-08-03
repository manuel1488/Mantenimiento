using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Shop;
using App.Core.DTOs.Shop.Calculation;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Inventory;
using App.Services.Settings;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class SaleService : IContextualSaleService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<SaleService> _logger;
    private readonly IStringLocalizer<SaleService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IDiscountSettingsService _discountSettingsService;
    private readonly IDiscountAuthorizerService _discountAuthorizerService;
    private readonly IContextualInventoryService _inventoryService;
    private readonly ITaxRateService _taxRateService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ITaxSettingsService _taxSettingsService;
    private readonly IProductPartialSurchargeService _productPartialSurchargeService;
    private readonly IRoundingSettingsService _roundingSettingsService;
    private readonly ICashRegisterService _cashRegisterService;
    private readonly IPricingCalculationService _pricingService;

    public SaleService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<SaleService> logger,
        IStringLocalizer<SaleService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IDiscountSettingsService discountSettingsService,
        IDiscountAuthorizerService discountAuthorizerService,
        IContextualInventoryService inventoryService,
        ITaxRateService taxRateService,
        ICompanySettingsService companySettingsService,
        ITaxSettingsService taxSettingsService,
        IProductPartialSurchargeService productPartialSurchargeService,
        IRoundingSettingsService roundingSettingsService,
        ICashRegisterService cashRegisterService,
        IPricingCalculationService pricingService)
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
        _cashRegisterService = cashRegisterService;
        _pricingService = pricingService;
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
        int? locationId = null,
        long? saleId = null,
        string? paymentMethodName = null,
        decimal? minTotal = null,
        decimal? maxTotal = null,
        string? customerNameFilter = null,
        string? createdByFilter = null,
        string? sortColumn = null,
        bool sortDescending = true)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Sale> query = context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Location)
                .Include(s => s.Quotation)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .Include(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                if (long.TryParse(searchString.Trim(), out var searchId))
                    query = query.Where(s => s.Id == searchId);
                else
                    query = query.Where(s => s.Customer.Name.Contains(searchString));
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

            if (saleId.HasValue)
            {
                query = query.Where(s => s.Id == saleId.Value);
            }

            if (!string.IsNullOrWhiteSpace(customerNameFilter))
            {
                query = query.Where(s => s.Customer.Name.Contains(customerNameFilter));
            }

            if (!string.IsNullOrWhiteSpace(createdByFilter))
            {
                query = query.Where(s => s.CreatedBy != null && s.CreatedBy.Contains(createdByFilter));
            }

            if (!string.IsNullOrWhiteSpace(paymentMethodName))
            {
                query = query.Where(s => s.Payments.Any(p => p.PaymentMethod.Name.Contains(paymentMethodName)));
            }

            if (minTotal.HasValue)
            {
                query = query.Where(s => s.Total >= minTotal.Value);
            }

            if (maxTotal.HasValue)
            {
                query = query.Where(s => s.Total <= maxTotal.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting — dynamic column sort with fallback to SaleDate desc
            var orderedQuery = sortColumn switch
            {
                "Id" => sortDescending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
                "SaleDate" => sortDescending ? query.OrderByDescending(s => s.SaleDate) : query.OrderBy(s => s.SaleDate),
                "CustomerName" => sortDescending ? query.OrderByDescending(s => s.Customer.Name) : query.OrderBy(s => s.Customer.Name),
                "Total" => sortDescending ? query.OrderByDescending(s => s.Total) : query.OrderBy(s => s.Total),
                "Status" => sortDescending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
                "CreatedBy" => sortDescending ? query.OrderByDescending(s => s.CreatedBy) : query.OrderBy(s => s.CreatedBy),
                _ => query.OrderByDescending(s => s.SaleDate)
            };

            // Apply pagination
            var items = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var salesDtos = items.Select(s => _mapper.Map<SaleDto>(s)).ToList();

            // Populate RemissionNumbers for consolidated sales (secondary query — no reverse nav)
            var consolidatedSaleIds = salesDtos
                .Where(s => s.SaleType == SaleType.Remission)
                .Select(s => s.Id)
                .ToList();

            if (consolidatedSaleIds.Count > 0)
            {
                var remissionsBySaleId = await context.Set<Remission>()
                    .AsNoTracking()
                    .Where(r => r.ConsolidatedSaleId != null && consolidatedSaleIds.Contains(r.ConsolidatedSaleId!.Value))
                    .GroupBy(r => r.ConsolidatedSaleId!.Value)
                    .ToDictionaryAsync(g => g.Key, g => string.Join(", ", g.OrderBy(r => r.Id).Select(r => r.RemissionNumber)));

                foreach (var dto in salesDtos)
                {
                    if (remissionsBySaleId.TryGetValue(dto.Id, out var numbers))
                        dto.RemissionNumbers = numbers;
                }
            }

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
                .Include(s => s.Quotation)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .Include(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
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
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await CreateSaleInternalAsync(createDto, context);
                if (result.IsSuccess)
                    await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating sale");
                return Result<SaleDto>.Failure(L["An error occurred while creating the sale: {0}", ex.Message]);
            }
        });
    }

    public async Task<Result<SaleDto>> CreateSaleAsync(
        CreateSaleDto createDto,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        return await CreateSaleInternalAsync(createDto, context, cancellationToken);
    }

    private async Task<Result<SaleDto>> CreateSaleInternalAsync(
        CreateSaleDto createDto,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate system configurations first
            var configValidation = await ValidateSystemConfigurationsAsync();
            if (!configValidation.IsSuccess)
            {
                return Result<SaleDto>.Failure(configValidation.Error!);
            }

            // Validate source quotation status
            if (createDto.QuotationId.HasValue)
            {
                var sourceQuotation = await context.Quotations
                    .Include(q => q.Details)
                    .FirstOrDefaultAsync(q => q.Id == createDto.QuotationId.Value);
                if (sourceQuotation is null)
                    return Result<SaleDto>.Failure(L["Quotation not found"]);
                if (sourceQuotation.Status == App.Core.Enums.Shop.QuotationStatus.ConvertedToSale ||
                    sourceQuotation.Status == App.Core.Enums.Shop.QuotationStatus.ConvertedToRemission)
                    return Result<SaleDto>.Failure(L["This quotation has already been converted"]);
                if (sourceQuotation.Status != App.Core.Enums.Shop.QuotationStatus.Accepted)
                    return Result<SaleDto>.Failure(L["Only accepted quotations can be converted to a sale"]);

                // Block conversion if the tax rate changed since the quotation was created.
                // The sale recomputes tax at the current rate, so honoring an outdated
                // quoted total would misstate tax — a fiscal compliance risk. A new
                // quotation must be created at the current rate.
                var quotedTaxRate = sourceQuotation.Details
                    .Where(d => d.TaxRate > 0)
                    .Select(d => (decimal?)d.TaxRate)
                    .FirstOrDefault();
                if (quotedTaxRate.HasValue)
                {
                    var currentRate = await _taxRateService.GetEffectiveRateAsync("MX");
                    if (Math.Abs(quotedTaxRate.Value - currentRate) >= 0.0001m)
                        return Result<SaleDto>.Failure(
                            L["Cannot convert: the tax rate changed since this quotation was created (quoted {0}, current {1}). Please create a new quotation.",
                                quotedTaxRate.Value.ToString("P2"), currentRate.ToString("P2")]);
                }
            }

            // Validate active cash register for current user+location
            long? cashRegisterId = null;
            var saleLocationId = createDto.LocationId;
            if (saleLocationId.HasValue)
            {
                var cashRegResult = await _cashRegisterService.GetActiveCashRegisterAsync(
                    saleLocationId.Value,
                    await _currentUserService.GetUserIdAsync());

                if (!cashRegResult.IsSuccess)
                    return Result<SaleDto>.Failure(cashRegResult.Error!);

                if (cashRegResult.Value == null)
                    return Result<SaleDto>.Failure(L["No open cash register found. Please open a cash register before processing sales."]);

                cashRegisterId = cashRegResult.Value.Id;

                // --- Cash limit strict-mode check ---
                var cashLimitSettings = await _cashRegisterService.GetSettingsAsync();
                if (cashLimitSettings.IsSuccess &&
                    cashLimitSettings.Value is { IsStrictCashLimit: true } &&
                    cashLimitSettings.Value.MaxCashLimit.HasValue)
                {
                    var cashMethodIds = await context.PaymentMethods
                        .AsNoTracking()
                        .Where(pm => pm.Type == App.Core.Enums.Shop.PaymentMethodType.Cash)
                        .Select(pm => pm.Id)
                        .ToListAsync();

                    var cashPaymentAmount = createDto.Payments
                        .Where(p => cashMethodIds.Contains(p.PaymentMethodId))
                        .Sum(p => p.Amount);

                    if (cashPaymentAmount > 0 &&
                        cashRegResult.Value.ExpectedCash >= cashLimitSettings.Value.MaxCashLimit.Value)
                    {
                        return Result<SaleDto>.Failure(
                            L["Cash register limit reached ({0:C}). Please make a withdrawal before accepting more cash payments.",
                              cashLimitSettings.Value.MaxCashLimit.Value]);
                    }
                }
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
            var saleCalculation = await CalculateSaleAsync(context, createDto, locationId, cashRegisterId);
            if (!saleCalculation.IsSuccess)
            {
                return Result<SaleDto>.Failure(saleCalculation.Error!);
            }

            var (sale, detailsToProcess) = saleCalculation.Value!;

            // Validate payments sum equals total
            var paymentsTotal = createDto.Payments.Sum(p => p.Amount);
            _logger.LogInformation(
                "Payment validation: paymentsTotal={PaymentsTotal}, saleTotal={SaleTotal}, subtotal={Subtotal}, tax={Tax}, discount={Discount}, rounding={Rounding}",
                paymentsTotal, sale.Total, sale.Subtotal, sale.TaxAmount, sale.DiscountAmount, sale.RoundingAmount);
            if (paymentsTotal < sale.Total)
            {
                return Result<SaleDto>.Failure(
                    L["Payment total ({0:C}) is less than sale total ({1:C})", paymentsTotal, sale.Total]);
            }

            // Attach payment entries
            var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";
            var now = _dateTime.Now;
            foreach (var paymentDto in createDto.Payments)
            {
                sale.Payments.Add(new App.Models.Shop.SalePayment
                {
                    PaymentMethodId = paymentDto.PaymentMethodId,
                    Amount = paymentDto.Amount,
                    CardLastFour = paymentDto.CardLastFour,
                    AuthorizationCode = paymentDto.AuthorizationCode,
                    CardBrand = paymentDto.CardBrand,
                    Reference = paymentDto.Reference,
                    CashTendered = paymentDto.CashTendered,
                    CashChange = paymentDto.CashChange,
                    CreatedBy = currentUser,
                    CreatedAt = now,
                    ModifiedBy = currentUser,
                    ModifiedAt = now
                });
            }

            // Mark source quotation as converted to sale (same SaveChangesAsync = atomic)
            if (createDto.QuotationId.HasValue)
            {
                var quotation = await context.Quotations.FindAsync(createDto.QuotationId.Value);
                if (quotation is not null)
                {
                    quotation.Status = App.Core.Enums.Shop.QuotationStatus.ConvertedToSale;
                    quotation.ModifiedBy = currentUser;
                    quotation.ModifiedAt = now;
                }
            }

            // Save the sale entity
            context.Sales.Add(sale);
            await context.SaveChangesAsync();

            // Process inventory for each detail that requires inventory tracking
            // Skip for remission-consolidated sales (inventory already deducted at remission time)
            if (createDto.SaleType != SaleType.Remission)
            {
                var saleProductIds = detailsToProcess.Select(d => d.ProductId).ToList();
                var productInventoryFlags = await context.Products
                    .AsNoTracking()
                    .Where(p => saleProductIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.RequiresInventory })
                    .ToDictionaryAsync(p => p.Id, p => p.RequiresInventory);

                foreach (var detail in detailsToProcess)
                {
                    if (productInventoryFlags.TryGetValue(detail.ProductId, out var requiresInventory) && !requiresInventory)
                        continue;

                    var movementResult = await _inventoryService.CreateMovementAsync(new CreateInventoryMovementDto
                    {
                        ProductId = detail.ProductId,
                        LocationId = locationId ?? 0, // TODO: Validate locationId properly
                        Quantity = detail.Quantity,
                        MovementType = InventoryMovementType.Sale,
                        MovementSubType = InventoryMovementSubType.DirectSale,
                        Reference = $"Sale-{sale.Id}",
                        Reason = $"Sale of {detail.Quantity} units"
                    }, context);

                    if (!movementResult.Success)
                    {
                        throw new InvalidOperationException(
                            L["Error processing inventory: {0}", movementResult.Message ?? "Unknown error"]);
                    }
                }
            }

            return Result<SaleDto>.Success(_mapper.Map<SaleDto>(sale));
        }
        catch (Exception ex)
        {
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
            sale.ModifiedBy = await _currentUserService.GetFullNameAsync();
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
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
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
                var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";
                var now = _dateTime.Now;

                if (sale.SaleType == SaleType.Remission)
                {
                    // For remission-consolidated sales, inventory is owned by the remissions.
                    // Revert associated remissions back to Pending so they can be cancelled individually.
                    var remissions = await context.Remissions
                        .Where(r => r.ConsolidatedSaleId == sale.Id)
                        .ToListAsync();

                    foreach (var remission in remissions)
                    {
                        remission.Status = RemissionStatus.Active;
                        remission.ConsolidatedSaleId = null;
                        remission.ConsolidatedAt = null;
                        remission.ConsolidatedBy = null;
                        remission.ModifiedBy = currentUser;
                        remission.ModifiedAt = now;
                    }
                }
                else
                {
                    // Return inventory for products that require inventory tracking
                    var cancelProductIds = sale.Details.Select(d => d.ProductId).ToList();
                    var cancelInventoryFlags = await context.Products
                        .AsNoTracking()
                        .Where(p => cancelProductIds.Contains(p.Id))
                        .Select(p => new { p.Id, p.RequiresInventory })
                        .ToDictionaryAsync(p => p.Id, p => p.RequiresInventory);

                    foreach (var detail in sale.Details)
                    {
                        if (cancelInventoryFlags.TryGetValue(detail.ProductId, out var requiresInventory) && !requiresInventory)
                            continue;

                        var movementResult = await _inventoryService.CreateMovementAsync(new CreateInventoryMovementDto
                        {
                            ProductId = detail.ProductId,
                            LocationId = locationId ?? 0,
                            Quantity = detail.Quantity,
                            MovementType = InventoryMovementType.Return,
                            MovementSubType = InventoryMovementSubType.DirectSale,
                            Reference = $"Cancel-Sale-{sale.Id}",
                            Reason = reason
                        }, context);

                        if (!movementResult.Success)
                        {
                            throw new InvalidOperationException(
                                L["Error returning inventory: {0}", movementResult.Message ?? "Unknown error"]);
                        }
                    }
                }

                // Update sale status
                sale.Status = App.Core.Enums.Shop.SaleStatus.Cancelled;
                sale.CancellationReason = reason;
                sale.ModifiedBy = await _currentUserService.GetFullNameAsync();
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
        });
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
        int? locationId,
        long? cashRegisterId = null)
    {
        try
        {
            // Validate locationId is provided
            if (!locationId.HasValue)
            {
                return Result<(Sale, List<SaleDetail>)>.Failure(
                    L["Location is required for sales"]);
            }

            var currentUserFullName = await _currentUserService.GetFullNameAsync();

            // Create new sale entity
            var sale = new Sale
            {
                CustomerId = createDto.CustomerId,
                SaleDate = createDto.SaleDate ?? _dateTime.Now,
                Status = App.Core.Enums.Shop.SaleStatus.Created,
                SaleType = createDto.SaleType,
                LocationId = createDto.LocationId,
                CashRegisterId = cashRegisterId,
                DiscountPercentage = createDto.DiscountPercentage,
                DiscountAuthorizedBy = createDto.DiscountAuthorizedBy,
                QuotationId = createDto.QuotationId,
                CreatedBy = currentUserFullName,
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

            // Check stock availability for all products that require inventory
            // Skip for remission-consolidated sales (inventory already deducted at remission time)
            if (createDto.SaleType != SaleType.Remission)
            {
                var insufficientStockProducts = new List<string>();
                foreach (var detailDto in createDto.Details)
                {
                    var product = products[detailDto.ProductId];
                    if (!product.RequiresInventory)
                        continue;

                    var stockAvailable = await _inventoryService.ValidateStockAvailabilityAsync(
                        detailDto.ProductId, locationId.Value, detailDto.Quantity);

                    if (!stockAvailable)
                        insufficientStockProducts.Add(product.Name);
                }

                if (insufficientStockProducts.Any())
                {
                    return Result<(Sale, List<SaleDetail>)>.Failure(
                        L["Insufficient stock for products: {0}",
                        string.Join(", ", insufficientStockProducts)]);
                }
            }

            // Get tax rate
            decimal defaultTaxRate = await _taxRateService.GetEffectiveRateAsync("MX"); // Assuming Mexico for now

            // Calculate all details and totals
            decimal subtotal = 0;
            decimal taxAmount = 0;
            decimal discountAmount = 0;
            var detailsToProcess = new List<SaleDetail>();

            var documentLines = new List<DocumentLineInput>();

            foreach (var detailDto in createDto.Details)
            {
                var product = products[detailDto.ProductId];
                decimal taxRate = product.IsTaxable ? defaultTaxRate : 0;

                // Determine effective unit price and surcharge from partial sale logic.
                // When IsCustomPrice is set (converting from a quotation with a locked
                // price, or a manual price override), use the provided UnitPrice directly
                // and skip the fractional recalculation. Otherwise the sale total would
                // diverge from the locked/quoted price — recomputing partial-sale lines
                // from the current catalog price instead of the quoted price — and trigger
                // "payment total is less than sale total" mismatches on conversion.
                decimal effectiveUnitPrice;
                decimal surchargePercentage = 0;
                decimal surchargeAmount = 0;
                decimal basePriceBeforeSurcharge = 0;
                int? partialSaleFractionId = null;

                if (product.IsPartialSaleAllowed && product.Content > 0 && !detailDto.IsCustomPrice)
                {
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
                        surchargePercentage = calc.SurchargePercentage;
                        surchargeAmount = Math.Round(calc.SurchargeAmount, 2);
                        basePriceBeforeSurcharge = calc.BasePriceBeforeSurcharge;
                        partialSaleFractionId = calc.FractionId;
                    }
                    else
                    {
                        effectiveUnitPrice = product.Price / product.Content;
                        basePriceBeforeSurcharge = effectiveUnitPrice * detailDto.Quantity;
                    }
                }
                else
                {
                    effectiveUnitPrice = detailDto.UnitPrice ?? product.Price;
                    basePriceBeforeSurcharge = effectiveUnitPrice * detailDto.Quantity;
                    partialSaleFractionId = detailDto.PartialSaleFractionId;
                }

                // Use centralized pricing service for line calculation
                var lineCalc = _pricingService.CalculateLine(new LineCalculationInput
                {
                    Quantity = detailDto.Quantity,
                    UnitPrice = effectiveUnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    DiscountAmount = detailDto.DiscountAmount,
                    SurchargePercentage = surchargePercentage,
                    TaxRate = taxRate
                });

                // Store with full 6-decimal precision from CalculateLine.
                // DB columns are decimal(10,6). CFDI reads these directly.
                var detailSubtotal = lineCalc.Subtotal;
                var detailDiscountAmount = lineCalc.DiscountAmount;
                var detailTotal = lineCalc.TaxBase + lineCalc.TaxAmount;

                var detail = new SaleDetail
                {
                    ProductId = detailDto.ProductId,
                    Quantity = detailDto.Quantity,
                    UnitPrice = effectiveUnitPrice,
                    DiscountPercentage = detailDto.DiscountPercentage,
                    DiscountAmount = detailDiscountAmount,
                    TaxRate = taxRate,
                    TaxAmount = lineCalc.TaxAmount,
                    Subtotal = detailSubtotal,
                    Total = detailTotal,
                    PartialSaleFractionId = partialSaleFractionId,
                    SurchargePercentage = surchargePercentage,
                    SurchargeAmount = surchargeAmount,
                    BasePriceBeforeSurcharge = basePriceBeforeSurcharge,
                    CreatedBy = currentUserFullName,
                    CreatedAt = _dateTime.Now
                };

                sale.Details.Add(detail);
                detailsToProcess.Add(detail);

                documentLines.Add(new DocumentLineInput
                {
                    Subtotal = lineCalc.Subtotal,
                    DiscountAmount = lineCalc.DiscountAmount,
                    IsTaxable = product.IsTaxable,
                    TaxAmount = lineCalc.TaxAmount,
                    TaxBase = lineCalc.TaxBase
                });
            }

            // Use centralized service for document-level totals.
            // ApplyRounding is decided explicitly by the caller (see CreateSaleDto.ApplyRounding) —
            // callers converting a frozen document (quotation, consolidated remission) must set it
            // to false so the already-paid locked total isn't rounded up after the fact.
            var docCalc = await _pricingService.CalculateDocumentAsync(new DocumentCalculationInput
            {
                Lines = documentLines,
                GlobalDiscountPercentage = createDto.DiscountPercentage,
                TaxRate = defaultTaxRate,
                ApplyRounding = createDto.ApplyRounding
            });

            // FrozenTotals (set by callers converting an already-paid, frozen document — a
            // consolidated remission) always wins over the recalculated totals: line items only
            // persist 2-decimal precision, so re-deriving Subtotal/Discount/Tax from them can
            // drift a cent from the amount the customer already paid.
            if (createDto.FrozenTotals is { } frozen)
            {
                sale.Subtotal = frozen.Subtotal;
                sale.TaxAmount = frozen.TaxAmount;
                sale.DiscountAmount = frozen.DiscountAmount;
                sale.RoundingAmount = 0;
                sale.Total = frozen.Total;
            }
            else
            {
                sale.Subtotal = docCalc.Subtotal;
                sale.TaxAmount = docCalc.TaxAmount;
                sale.DiscountAmount = docCalc.TotalDiscountAmount;
                sale.RoundingAmount = docCalc.RoundingAmount;
                sale.Total = docCalc.Total;
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

    public async Task<Result<Dictionary<long, SaleCancellationStatusDto>>> GetCancellationStatusAsync(IEnumerable<long> saleIds)
    {
        try
        {
            var ids = saleIds.ToList();
            if (ids.Count == 0)
                return Result<Dictionary<long, SaleCancellationStatusDto>>.Success([]);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var invoicedIds = await context.MexicoInvoices
                .AsNoTracking()
                .Where(i => ids.Contains(i.SaleId) && i.Status != "Cancelled" && i.Status != "StampError")
                .Select(i => i.SaleId)
                .ToHashSetAsync();

            var globalMap = await context.GlobalInvoiceSales
                .AsNoTracking()
                .Where(gs => ids.Contains(gs.SaleId) && gs.GlobalInvoice!.Status == GlobalInvoiceStatus.Stamped)
                .Select(gs => new { gs.SaleId, gs.GlobalInvoiceId })
                .ToDictionaryAsync(x => x.SaleId, x => x.GlobalInvoiceId);

            var result = ids.ToDictionary(id => id, id =>
            {
                var blockedByInvoice = invoicedIds.Contains(id);
                var blockedByGlobal = globalMap.TryGetValue(id, out var globalId);
                return new SaleCancellationStatusDto
                {
                    CanCancel = !blockedByInvoice && !blockedByGlobal,
                    BlockedByInvoice = blockedByInvoice,
                    BlockedByGlobalInvoice = blockedByGlobal,
                    GlobalInvoiceId = blockedByGlobal ? globalId : null
                };
            });

            return Result<Dictionary<long, SaleCancellationStatusDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cancellation status for sales");
            return Result<Dictionary<long, SaleCancellationStatusDto>>.Failure(L["Error loading cancellation status"]);
        }
    }
}
