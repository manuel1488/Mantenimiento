using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class PhysicalInventoryCountService : IPhysicalInventoryCountService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<PhysicalInventoryCountService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<PhysicalInventoryCountService> L;
    private readonly IDateTime _dateTime;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IPdfService _pdfService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IDocumentSequenceService _documentSequenceService;

    public PhysicalInventoryCountService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<PhysicalInventoryCountService> logger,
        ICurrentUserService currentUserService,
        IStringLocalizer<PhysicalInventoryCountService> localizer,
        IDateTime dateTime,
        ICompanySettingsService companySettingsService,
        IPdfService pdfService,
        IEmailTemplateService emailTemplateService,
        IDocumentSequenceService documentSequenceService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _currentUserService = currentUserService;
        L = localizer;
        _dateTime = dateTime;
        _companySettingsService = companySettingsService;
        _pdfService = pdfService;
        _emailTemplateService = emailTemplateService;
        _documentSequenceService = documentSequenceService;
    }

    public async Task<WarehouseStockDto> GetCountSheetAsync(
        int locationId,
        CancellationToken cancellationToken = default)
    {
        if (!await _currentUserService.HasAccessToLocationAsync(locationId))
            throw new InvalidOperationException($"Location not found: {locationId}");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var location = await context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == locationId, cancellationToken);

        if (location == null)
            throw new InvalidOperationException($"Location not found: {locationId}");

        var inventory = await context.Inventory
            .Include(x => x.Product)
            .ThenInclude(x => x.UnitMeasure)
            .Where(x => x.LocationId == locationId && x.Product.IsActive)
            .OrderBy(x => x.Product.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new WarehouseStockDto
        {
            LocationId = location.Id,
            LocationName = location.Name,
            LocationType = location.Type,
            TotalProducts = inventory.Count,
            ProductsWithStock = inventory.Count(x => x.Quantity > 0),
            ProductsBelowMinStock = inventory.Count(x =>
                x.MinStock.HasValue && x.Quantity < x.MinStock.Value),
            ProductsAboveMaxStock = inventory.Count(x =>
                x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value),
            ProductStock = inventory.Select(x => new WarehouseProductStockDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                ProductCode = x.Product.Code,
                UnitMeasureName = x.Product.UnitMeasure.Name,
                Quantity = x.Quantity,
                MinStock = x.MinStock,
                MaxStock = x.MaxStock,
                IsBelowMinStock = x.MinStock.HasValue && x.Quantity < x.MinStock.Value,
                IsAboveMaxStock = x.MaxStock.HasValue && x.Quantity > x.MaxStock.Value
            }).ToList()
        };
    }

    public async Task<InventoryOperationResult<PhysicalInventoryCountResultDto>> CreateAndApplyAsync(
        CreatePhysicalInventoryCountDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!await _currentUserService.HasAccessToLocationAsync(dto.LocationId))
        {
            return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(
                L["You don't have access to this location"]);
        }

        if (dto.Lines.Count == 0)
        {
            return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(
                L["At least one counted line is required"]);
        }

        if (dto.Lines.Select(l => l.ProductId).Distinct().Count() != dto.Lines.Count)
        {
            return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(
                L["Duplicate products are not allowed in the same count"]);
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var location = await context.Locations
            .FirstOrDefaultAsync(x => x.Id == dto.LocationId && x.IsActive, cancellationToken);

        if (location == null)
        {
            return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(
                L["Invalid or inactive location"]);
        }

        var productIds = dto.Lines.Select(l => l.ProductId).ToList();

        var inventories = await context.Inventory
            .Include(x => x.Product)
            .Where(x => x.LocationId == dto.LocationId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        var lineErrors = new List<PhysicalCountLineResultDto>();
        var validLines = new List<(PhysicalCountLineDto Line, App.Models.Shop.Inventory Inventory)>();

        foreach (var line in dto.Lines)
        {
            var inventory = inventories.FirstOrDefault(x => x.ProductId == line.ProductId);

            if (inventory == null || !inventory.Product.IsActive)
            {
                lineErrors.Add(new PhysicalCountLineResultDto
                {
                    ProductId = line.ProductId,
                    ProductCode = inventory?.Product.Code ?? line.ProductId.ToString(),
                    ProductName = inventory?.Product.Name ?? "-",
                    CountedQuantity = line.CountedQuantity,
                    MovementApplied = false
                });
                continue;
            }

            if (line.CountedQuantity < 0)
            {
                lineErrors.Add(new PhysicalCountLineResultDto
                {
                    ProductId = line.ProductId,
                    ProductCode = inventory.Product.Code,
                    ProductName = inventory.Product.Name,
                    SystemQuantity = inventory.Quantity,
                    CountedQuantity = line.CountedQuantity,
                    MovementApplied = false
                });
                continue;
            }

            validLines.Add((line, inventory));
        }

        if (lineErrors.Count > 0)
        {
            return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(
                L["One or more lines are invalid; no count was applied"]);
        }

        var countDate = _dateTime.Now;
        var createdBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
        var batchId = Guid.NewGuid();

        var strategy = context.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var batchNumber = await _documentSequenceService.GetNextNumberAsync(
                    context, "PhysicalInventoryCount", "PIC", countDate.Year);

                var count = new PhysicalInventoryCount
                {
                    BatchNumber = batchNumber,
                    BatchId = batchId,
                    LocationId = dto.LocationId,
                    Reason = dto.Reason,
                    Reference = dto.Reference,
                    CountDate = countDate,
                    CreatedBy = createdBy,
                    CreatedAt = countDate
                };

                context.PhysicalInventoryCounts.Add(count);

                var lineResults = new List<PhysicalCountLineResultDto>();

                foreach (var (line, inventory) in validLines)
                {
                    var previousBalance = inventory.Quantity;
                    var difference = line.CountedQuantity - previousBalance;

                    var countLine = new PhysicalInventoryCountLine
                    {
                        PhysicalInventoryCount = count,
                        ProductId = line.ProductId,
                        SystemQuantity = previousBalance,
                        CountedQuantity = line.CountedQuantity,
                        Difference = difference,
                        CreatedBy = createdBy,
                        CreatedAt = countDate
                    };

                    if (difference != 0)
                    {
                        inventory.Quantity = line.CountedQuantity;
                        inventory.ModifiedBy = createdBy;

                        var movement = new InventoryMovement
                        {
                            ProductId = line.ProductId,
                            LocationId = dto.LocationId,
                            MovementType = InventoryMovementType.Adjustment,
                            MovementSubType = InventoryMovementSubType.PhysicalCount,
                            Quantity = Math.Abs(difference),
                            Reference = dto.Reference,
                            Reason = dto.Reason,
                            PreviousBalance = previousBalance,
                            NewBalance = line.CountedQuantity,
                            MovementDate = countDate,
                            BatchId = batchId,
                            BatchNumber = batchNumber,
                            CreatedBy = createdBy,
                            CreatedAt = countDate
                        };

                        context.InventoryMovements.Add(movement);
                        countLine.InventoryMovement = movement;
                    }

                    context.PhysicalInventoryCountLines.Add(countLine);

                    lineResults.Add(new PhysicalCountLineResultDto
                    {
                        ProductId = line.ProductId,
                        ProductCode = inventory.Product.Code,
                        ProductName = inventory.Product.Name,
                        SystemQuantity = previousBalance,
                        CountedQuantity = line.CountedQuantity,
                        Difference = difference,
                        MovementApplied = difference != 0
                    });
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return InventoryOperationResult<PhysicalInventoryCountResultDto>.Ok(new PhysicalInventoryCountResultDto
                {
                    Id = count.Id,
                    BatchId = batchId,
                    BatchNumber = batchNumber,
                    LocationId = dto.LocationId,
                    LocationName = location.Name,
                    CountDate = countDate,
                    CreatedBy = createdBy,
                    Reason = dto.Reason,
                    Reference = dto.Reference,
                    Lines = lineResults
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Concurrency error applying physical inventory count");
                return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(
                    L["The inventory was modified by another process"]);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error applying physical inventory count");
                return InventoryOperationResult<PhysicalInventoryCountResultDto>.Error(ex.Message);
            }
        });

        return result;
    }

    public async Task<(int TotalCount, IList<PhysicalInventoryCountResultDto> Items)> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        int? locationId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<PhysicalInventoryCount> query = context.PhysicalInventoryCounts
            .Include(x => x.Location)
            .Include(x => x.Lines)
            .ThenInclude(l => l.Product)
            .OrderByDescending(x => x.CreatedAt);

        if (locationId.HasValue)
        {
            if (!await _currentUserService.HasAccessToLocationAsync(locationId.Value))
                return (0, new List<PhysicalInventoryCountResultDto>());

            query = query.Where(x => x.LocationId == locationId.Value);
        }
        else if (!await _currentUserService.GetIsGlobalAccessAsync())
        {
            var assignedIds = await _currentUserService.GetAssignedLocationIdsAsync();
            query = query.Where(x => assignedIds.Contains(x.LocationId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var counts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (totalCount, counts.Select(MapToResultDto).ToList());
    }

    public async Task<PhysicalInventoryCountResultDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var count = await context.PhysicalInventoryCounts
            .Include(x => x.Location)
            .Include(x => x.Lines)
            .ThenInclude(l => l.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return count == null ? null : MapToResultDto(count);
    }

    public async Task<byte[]> GeneratePdfAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var count = await context.PhysicalInventoryCounts
            .Include(x => x.Location)
            .Include(x => x.Lines)
            .ThenInclude(l => l.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BatchId == batchId, cancellationToken);

        if (count == null)
        {
            throw new InvalidOperationException($"Physical inventory count batch {batchId} not found");
        }

        var companySettings = await _companySettingsService.GetSettingsAsync();
        var (logoBytes, logoMime) = await _emailTemplateService.GetStaticFileBytesAsync("images/logo.webp");
        var logoBase64 = logoBytes.Length > 0
            ? $"data:{logoMime};base64,{Convert.ToBase64String(logoBytes)}"
            : string.Empty;

        var model = new PhysicalCountPdfDto
        {
            CompanyName = companySettings?.CompanyName ?? "Cleeny",
            LogoBase64 = logoBase64,
            BatchId = batchId,
            BatchNumber = count.BatchNumber,
            LocationName = count.Location.Name,
            Reason = count.Reason,
            Reference = count.Reference,
            CountDate = count.CountDate,
            CreatedBy = count.CreatedBy,
            Lines = count.Lines.Select(l => new PhysicalCountPdfLineDto
            {
                ProductCode = l.Product.Code,
                ProductName = l.Product.Name,
                SystemQuantity = l.SystemQuantity,
                CountedQuantity = l.CountedQuantity,
                Difference = l.Difference
            }).ToList()
        };

        return await _pdfService.GeneratePdfFromViewAsync(
            "~/Views/PhysicalCounts/PhysicalCountDocument.cshtml", model, cancellationToken);
    }

    private static PhysicalInventoryCountResultDto MapToResultDto(PhysicalInventoryCount count) => new()
    {
        Id = count.Id,
        BatchId = count.BatchId,
        BatchNumber = count.BatchNumber,
        LocationId = count.LocationId,
        LocationName = count.Location.Name,
        CountDate = count.CountDate,
        CreatedBy = count.CreatedBy,
        Reason = count.Reason,
        Reference = count.Reference,
        Lines = count.Lines.Select(l => new PhysicalCountLineResultDto
        {
            ProductId = l.ProductId,
            ProductCode = l.Product.Code,
            ProductName = l.Product.Name,
            SystemQuantity = l.SystemQuantity,
            CountedQuantity = l.CountedQuantity,
            Difference = l.Difference,
            MovementApplied = l.InventoryMovementId.HasValue
        }).ToList()
    };
}
