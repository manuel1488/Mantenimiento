using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Shop;
using App.Core.DTOs.Shop.Calculation;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
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

public class RemissionService : IRemissionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<RemissionService> _logger;
    private readonly IStringLocalizer<RemissionService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ITaxRateService _taxRateService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IContextualInventoryService _inventoryService;
    private readonly IPricingCalculationService _pricingService;
    private readonly IPdfService _pdfService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IContextualSaleService _saleService;
    private readonly IDocumentSequenceService _documentSequenceService;

    public RemissionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<RemissionService> logger,
        IStringLocalizer<RemissionService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ITaxRateService taxRateService,
        ICompanySettingsService companySettingsService,
        IContextualInventoryService inventoryService,
        IPricingCalculationService pricingService,
        IPdfService pdfService,
        IEmailTemplateService emailTemplateService,
        IContextualSaleService saleService,
        IDocumentSequenceService documentSequenceService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _taxRateService = taxRateService;
        _companySettingsService = companySettingsService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _pdfService = pdfService;
        _emailTemplateService = emailTemplateService;
        _saleService = saleService;
        _documentSequenceService = documentSequenceService;
    }

    public async Task<(int TotalCount, IList<RemissionDto> Items)> GetRemissionsAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        long? customerId = null,
        string? status = null,
        int? locationId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Remissions
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Location)
            .Include(r => r.Details)
            .Include(r => r.Quotation)
            .Where(r => r.IsDeleted == 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(r =>
                r.RemissionNumber.ToLower().Contains(lower) ||
                r.Customer.Name.ToLower().Contains(lower));
        }

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RemissionStatus>(status, true, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);

        if (locationId.HasValue)
            query = query.Where(r => r.LocationId == locationId.Value);

        if (startDate.HasValue)
            query = query.Where(r => r.RemissionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.RemissionDate <= endDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.RemissionDate)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, _mapper.Map<IList<RemissionDto>>(items));
    }

    public async Task<RemissionDto?> GetByIdAsync(long id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var remission = await context.Remissions
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Location)
            .Include(r => r.Details)
                .ThenInclude(d => d.Product)
            .Where(r => r.IsDeleted == 0 && r.Id == id)
            .FirstOrDefaultAsync();

        return remission is null ? null : _mapper.Map<RemissionDto>(remission);
    }

    public async Task<Result<RemissionDto>> CreateAsync(CreateRemissionDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Validate source quotation status
                    if (dto.QuotationId.HasValue)
                    {
                        var sourceQuotation = await context.Quotations
                            .Include(q => q.Details)
                            .FirstOrDefaultAsync(q => q.Id == dto.QuotationId.Value);
                        if (sourceQuotation is null)
                            return Result<RemissionDto>.Failure(_localizer["Quotation not found"]);
                        if (sourceQuotation.Status == App.Core.Enums.Shop.QuotationStatus.ConvertedToSale ||
                            sourceQuotation.Status == App.Core.Enums.Shop.QuotationStatus.ConvertedToRemission)
                            return Result<RemissionDto>.Failure(_localizer["This quotation has already been converted"]);
                        if (sourceQuotation.Status != App.Core.Enums.Shop.QuotationStatus.Accepted)
                            return Result<RemissionDto>.Failure(_localizer["Only accepted quotations can be converted to a remission"]);

                        // Block conversion if the tax rate changed since the quotation was
                        // created. The remission recomputes tax at the current rate, so
                        // honoring an outdated quoted total would misstate tax — a fiscal
                        // compliance risk. A new quotation must be created at the current rate.
                        var quotedTaxRate = sourceQuotation.Details
                            .Where(d => d.TaxRate > 0)
                            .Select(d => (decimal?)d.TaxRate)
                            .FirstOrDefault();
                        if (quotedTaxRate.HasValue)
                        {
                            var settings = await _companySettingsService.GetSettingsAsync();
                            var currentRate = await _taxRateService.GetEffectiveRateAsync(settings?.CountryCode ?? "MX");
                            if (Math.Abs(quotedTaxRate.Value - currentRate) >= 0.0001m)
                                return Result<RemissionDto>.Failure(
                                    _localizer["Cannot convert: the tax rate changed since this quotation was created (quoted {0}, current {1}). Please create a new quotation.",
                                        quotedTaxRate.Value.ToString("P2"), currentRate.ToString("P2")]);
                        }
                    }

                    var customer = await context.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.IsDeleted == 0);

                    if (customer is null)
                        return Result<RemissionDto>.Failure(_localizer["Customer not found"]);

                    var location = await context.Locations
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == dto.LocationId && l.IsDeleted == 0);

                    if (location is null)
                        return Result<RemissionDto>.Failure(_localizer["Location not found"]);

                    var now = _dateTime.Now;
                    var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";

                    var companySettings = await _companySettingsService.GetSettingsAsync();
                    var countryCode = companySettings?.CountryCode ?? "MX";
                    var taxRate = await _taxRateService.GetEffectiveRateAsync(countryCode);

                    // Validate products exist
                    var productIds = dto.Details.Select(d => d.ProductId).ToList();
                    var products = await context.Products
                        .AsNoTracking()
                        .Where(p => productIds.Contains(p.Id) && p.IsDeleted == 0)
                        .ToDictionaryAsync(p => p.Id);

                    foreach (var detailDto in dto.Details)
                    {
                        if (!products.ContainsKey(detailDto.ProductId))
                            return Result<RemissionDto>.Failure(
                                string.Format(_localizer["Product {0} not found"], detailDto.ProductId));
                    }

                    // Validate stock availability
                    var insufficientStockProducts = new List<string>();
                    foreach (var detailDto in dto.Details)
                    {
                        var product = products[detailDto.ProductId];
                        if (!product.RequiresInventory)
                            continue;

                        var stockAvailable = await _inventoryService.ValidateStockAvailabilityAsync(
                            detailDto.ProductId, dto.LocationId, detailDto.Quantity);

                        if (!stockAvailable)
                            insufficientStockProducts.Add(product.Name);
                    }

                    if (insufficientStockProducts.Count > 0)
                    {
                        return Result<RemissionDto>.Failure(
                            string.Format(_localizer["Insufficient stock for products: {0}"],
                                string.Join(", ", insufficientStockProducts)));
                    }

                    var remissionNumber = await _documentSequenceService.GetNextNumberAsync(context, "Remission", "REM", now.Year);

                    var remission = new Remission
                    {
                        RemissionNumber = remissionNumber,
                        CustomerId = dto.CustomerId,
                        RemissionDate = dto.RemissionDate ?? now,
                        LocationId = dto.LocationId,
                        Status = RemissionStatus.Active,
                        Notes = dto.Notes,
                        DiscountPercentage = dto.DiscountPercentage,
                        TaxRate = taxRate,
                        QuotationId = dto.QuotationId,
                        CreatedBy = currentUser,
                        CreatedAt = now,
                        ModifiedBy = currentUser,
                        ModifiedAt = now
                    };

                    var documentLines = new List<DocumentLineInput>();

                    foreach (var detailDto in dto.Details)
                    {
                        var product = products[detailDto.ProductId];

                        var lineCalc = _pricingService.CalculateLine(new LineCalculationInput
                        {
                            Quantity = detailDto.Quantity,
                            UnitPrice = detailDto.UnitPrice,
                            DiscountPercentage = detailDto.DiscountPercentage,
                            DiscountAmount = detailDto.DiscountAmount,
                            TaxRate = taxRate
                        });

                        var lineDiscount = Math.Round(lineCalc.DiscountAmount, 2);
                        var lineTotal = Math.Round(lineCalc.TaxBase + lineCalc.TaxAmount, 2);

                        var detail = new RemissionDetail
                        {
                            ProductId = detailDto.ProductId,
                            ProductName = product.Name,
                            ProductCode = product.Code ?? string.Empty,
                            Quantity = detailDto.Quantity,
                            UnitPrice = detailDto.UnitPrice,
                            DiscountPercentage = detailDto.DiscountPercentage,
                            DiscountAmount = lineDiscount,
                            TaxRate = taxRate,
                            TaxAmount = lineCalc.TaxAmount,
                            Subtotal = lineCalc.TaxBase,
                            Total = lineTotal,
                            CreatedBy = currentUser,
                            CreatedAt = now,
                            ModifiedBy = currentUser,
                            ModifiedAt = now
                        };

                        remission.Details.Add(detail);
                        documentLines.Add(new DocumentLineInput
                        {
                            Subtotal = lineCalc.Subtotal,
                            DiscountAmount = lineCalc.DiscountAmount,
                            IsTaxable = true,
                            TaxAmount = lineCalc.TaxAmount,
                            TaxBase = lineCalc.TaxBase
                        });
                    }

                    var docCalc = await _pricingService.CalculateDocumentAsync(new DocumentCalculationInput
                    {
                        Lines = documentLines,
                        GlobalDiscountPercentage = dto.DiscountPercentage,
                        TaxRate = taxRate,
                        ApplyRounding = false
                    });

                    remission.Subtotal = docCalc.Subtotal - docCalc.TotalDiscountAmount;
                    remission.DiscountAmount = docCalc.TotalDiscountAmount;
                    remission.TaxAmount = docCalc.TaxAmount;
                    remission.Total = docCalc.Total;

                    // Mark source quotation as converted to remission (same SaveChangesAsync = atomic)
                    if (dto.QuotationId.HasValue)
                    {
                        var quotation = await context.Quotations.FindAsync(dto.QuotationId.Value);
                        if (quotation is not null)
                        {
                            quotation.Status = App.Core.Enums.Shop.QuotationStatus.ConvertedToRemission;
                            quotation.ModifiedBy = currentUser;
                            quotation.ModifiedAt = now;
                        }
                    }

                    context.Remissions.Add(remission);
                    await context.SaveChangesAsync();

                    // Deduct inventory for products that require it
                    var remissionProductIds = dto.Details.Select(d => d.ProductId).ToList();
                    var productInventoryFlags = await context.Products
                        .AsNoTracking()
                        .Where(p => remissionProductIds.Contains(p.Id))
                        .Select(p => new { p.Id, p.RequiresInventory })
                        .ToDictionaryAsync(p => p.Id, p => p.RequiresInventory);

                    foreach (var detail in remission.Details)
                    {
                        if (productInventoryFlags.TryGetValue(detail.ProductId, out var requiresInventory) && !requiresInventory)
                            continue;

                        var movementResult = await _inventoryService.CreateMovementAsync(new CreateInventoryMovementDto
                        {
                            ProductId = detail.ProductId,
                            LocationId = dto.LocationId,
                            Quantity = detail.Quantity,
                            MovementType = InventoryMovementType.Sale,
                            MovementSubType = InventoryMovementSubType.Remission,
                            Reference = $"Remission-{remission.Id}",
                            Reason = $"Remission {remission.RemissionNumber} - {detail.Quantity} units"
                        }, context);

                        if (!movementResult.Success)
                        {
                            throw new InvalidOperationException(
                                string.Format(_localizer["Error processing inventory: {0}"],
                                    movementResult.Message ?? "Unknown error"));
                        }
                    }

                    await transaction.CommitAsync();

                    var created = await context.Remissions
                        .Include(r => r.Customer)
                        .Include(r => r.Location)
                        .Include(r => r.Quotation)
                        .Include(r => r.Details)
                        .FirstAsync(r => r.Id == remission.Id);

                    return Result<RemissionDto>.Success(_mapper.Map<RemissionDto>(created));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating remission");
            return Result<RemissionDto>.Failure(_localizer["Error creating remission"]);
        }
    }

    public async Task<Result> CancelAsync(long id, string reason)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var remission = await context.Remissions
                        .Include(r => r.Details)
                        .FirstOrDefaultAsync(r => r.Id == id && r.IsDeleted == 0);

                    if (remission is null)
                        return Result.Failure(_localizer["Remission not found"]);

                    if (remission.Status == RemissionStatus.Consolidated)
                        return Result.Failure(_localizer["Cannot cancel a consolidated remission. Cancel the associated sale first."]);

                    if (remission.Status != RemissionStatus.Active)
                        return Result.Failure(_localizer["Only pending remissions can be cancelled"]);

                    var now = _dateTime.Now;
                    var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";

                    // Revert inventory
                    var cancelProductIds = remission.Details.Select(d => d.ProductId).ToList();
                    var cancelInventoryFlags = await context.Products
                        .AsNoTracking()
                        .Where(p => cancelProductIds.Contains(p.Id))
                        .Select(p => new { p.Id, p.RequiresInventory })
                        .ToDictionaryAsync(p => p.Id, p => p.RequiresInventory);

                    foreach (var detail in remission.Details)
                    {
                        if (cancelInventoryFlags.TryGetValue(detail.ProductId, out var requiresInventory) && !requiresInventory)
                            continue;

                        var movementResult = await _inventoryService.CreateMovementAsync(new CreateInventoryMovementDto
                        {
                            ProductId = detail.ProductId,
                            LocationId = remission.LocationId,
                            Quantity = detail.Quantity,
                            MovementType = InventoryMovementType.Return,
                            MovementSubType = InventoryMovementSubType.CustomerOrder,
                            Reference = $"Remission-Cancel-{remission.Id}",
                            Reason = $"Cancelled remission {remission.RemissionNumber}"
                        }, context);

                        if (!movementResult.Success)
                        {
                            throw new InvalidOperationException(
                                string.Format(_localizer["Error reverting inventory: {0}"],
                                    movementResult.Message ?? "Unknown error"));
                        }
                    }

                    remission.Status = RemissionStatus.Cancelled;
                    remission.CancellationReason = reason;
                    remission.CancelledAt = now;
                    remission.ModifiedBy = currentUser;
                    remission.ModifiedAt = now;

                    // Clear cached PDF
                    remission.PdfData = null;
                    remission.PdfGeneratedAt = null;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling remission {Id}", id);
            return Result.Failure(_localizer["Error cancelling remission"]);
        }
    }

    public async Task<Result<List<RemissionDto>>> GetPendingByCustomerAsync(long customerId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var remissions = await context.Remissions
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Location)
                .Include(r => r.Details)
                .Where(r => r.IsDeleted == 0 &&
                            r.CustomerId == customerId &&
                            r.Status == RemissionStatus.Active)
                .OrderByDescending(r => r.RemissionDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            return Result<List<RemissionDto>>.Success(_mapper.Map<List<RemissionDto>>(remissions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending remissions for customer {CustomerId}", customerId);
            return Result<List<RemissionDto>>.Failure(_localizer["Error getting pending remissions"]);
        }
    }

    public async Task<Result<long>> ConsolidateAsync(ConsolidateRemissionsDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                // Load all specified remissions
                var remissions = await context.Remissions
                    .Include(r => r.Details)
                    .Where(r => dto.RemissionIds.Contains(r.Id) && r.IsDeleted == 0)
                    .ToListAsync();

                // Validate all remissions found
                if (remissions.Count != dto.RemissionIds.Count)
                {
                    var foundIds = remissions.Select(r => r.Id).ToHashSet();
                    var missingIds = dto.RemissionIds.Where(id => !foundIds.Contains(id));
                    return Result<long>.Failure(
                        string.Format(_localizer["Remissions not found: {0}"],
                            string.Join(", ", missingIds)));
                }

                // Validate all belong to the same customer
                if (remissions.Any(r => r.CustomerId != dto.CustomerId))
                    return Result<long>.Failure(
                        _localizer["All remissions must belong to the same customer"]);

                // Validate all are Pending
                var nonPending = remissions.Where(r => r.Status != RemissionStatus.Active).ToList();
                if (nonPending.Count > 0)
                    return Result<long>.Failure(
                        string.Format(_localizer["Remissions not in pending status: {0}"],
                            string.Join(", ", nonPending.Select(r => r.RemissionNumber))));

                // Block consolidation if the tax rate changed since a remission was created.
                // The consolidated sale recomputes tax at the current rate, so honoring an
                // outdated remission total would misstate tax and, since the customer already
                // paid the frozen remission total, also mismatch the payment validation.
                var currentRate = await _taxRateService.GetEffectiveRateAsync("MX");
                foreach (var remission in remissions)
                {
                    var remissionTaxRate = remission.Details
                        .Where(d => d.TaxRate > 0)
                        .Select(d => (decimal?)d.TaxRate)
                        .FirstOrDefault();
                    if (remissionTaxRate.HasValue && Math.Abs(remissionTaxRate.Value - currentRate) >= 0.0001m)
                        return Result<long>.Failure(
                            _localizer["Cannot consolidate: the tax rate changed since remission {0} was created (remission {1}, current {2}). Please cancel and recreate the remission.",
                                remission.RemissionNumber, remissionTaxRate.Value.ToString("P2"), currentRate.ToString("P2")]);
                }

                // Build consolidated sale details from all remission details
                var saleDetails = new List<CreateSaleDetailDto>();
                foreach (var remission in remissions)
                {
                    foreach (var detail in remission.Details)
                    {
                        saleDetails.Add(new CreateSaleDetailDto
                        {
                            ProductId = detail.ProductId,
                            Quantity = detail.Quantity,
                            UnitPrice = detail.UnitPrice,
                            DiscountPercentage = detail.DiscountPercentage,
                            DiscountAmount = detail.DiscountAmount > 0 ? detail.DiscountAmount : null
                        });
                    }
                }

                // Create consolidated sale (SaleType.Remission skips inventory deduction).
                // ApplyRounding=false: the remissions' totals were already frozen without
                // rounding (RemissionService.CreateAsync uses ApplyRounding=false) and the
                // customer already paid that exact amount.
                var createSaleDto = new CreateSaleDto
                {
                    CustomerId = dto.CustomerId,
                    SaleType = SaleType.Remission,
                    LocationId = dto.LocationId,
                    Payments = dto.Payments,
                    Details = saleDetails,
                    ApplyRounding = false
                };

                // Create sale and mark remissions as consolidated in one atomic transaction
                var saleResult = await _saleService.CreateSaleAsync(createSaleDto, context);
                if (!saleResult.IsSuccess)
                    return Result<long>.Failure(saleResult.Error!);

                var saleId = saleResult.Value!.Id;
                var now = _dateTime.Now;
                var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";

                foreach (var remission in remissions)
                {
                    remission.Status = RemissionStatus.Consolidated;
                    remission.ConsolidatedSaleId = saleId;
                    remission.ConsolidatedAt = now;
                    remission.ConsolidatedBy = currentUser;
                    remission.ModifiedBy = currentUser;
                    remission.ModifiedAt = now;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<long>.Success(saleId);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating remissions");
            return Result<long>.Failure(_localizer["Error consolidating remissions"]);
        }
    }

    public async Task<byte[]> GeneratePdfAsync(long id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var remission = await context.Remissions
            .Include(r => r.Customer)
                .ThenInclude(c => c.FiscalProfile)
            .Include(r => r.Location)
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsDeleted == 0)
            ?? throw new InvalidOperationException($"Remission {id} not found");

        // Return stored PDF if available
        if (remission.PdfData != null && remission.PdfGeneratedAt.HasValue)
            return remission.PdfData;

        // Generate, store, and return
        var pdfBytes = await GeneratePdfBytesAsync(remission);
        remission.PdfData = pdfBytes;
        remission.PdfGeneratedAt = _dateTime.Now;
        await context.SaveChangesAsync();

        return pdfBytes;
    }

    private async Task<byte[]> GeneratePdfBytesAsync(Remission remission)
    {
        var companySettings = await _companySettingsService.GetSettingsAsync();

        var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
        var logoBase64 = logoBytes.Length > 0
            ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
            : string.Empty;

        var c = remission.Customer;

        var addressParts = new[]
        {
            string.IsNullOrWhiteSpace(c.Street) ? null
                : c.Street + (!string.IsNullOrWhiteSpace(c.ExteriorNumber) ? $" #{c.ExteriorNumber}" : string.Empty),
            c.Neighborhood,
            c.City,
            c.State,
            c.PostalCode
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        var address = string.Join(", ", addressParts);

        var model = new RemissionPdfDto
        {
            RemissionNumber = remission.RemissionNumber,
            CustomerName = c.Name,
            CustomerLegalName = c.FiscalProfile?.LegalName,
            CustomerPhone = c.Phone,
            CustomerAddress = string.IsNullOrWhiteSpace(address) ? null : address,
            CustomerTaxId = c.FiscalProfile?.TaxId,
            RemissionDate = remission.RemissionDate,
            LocationName = remission.Location?.Name ?? string.Empty,
            Notes = remission.Notes,
            Subtotal = remission.Subtotal,
            DiscountPercentage = remission.DiscountPercentage,
            DiscountAmount = remission.DiscountAmount,
            TaxAmount = remission.TaxAmount,
            Total = remission.Total,
            Details = _mapper.Map<List<RemissionDetailDto>>(remission.Details),
            CompanyName = companySettings?.CompanyName ?? "Cleeny",
            LogoBase64 = logoBase64
        };

        return await _pdfService.GeneratePdfFromViewAsync("~/Views/Remissions/RemissionDocument.cshtml", model);
    }

}
