using App.Core.Common;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Inventory;

public class StockEntryService : IStockEntryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<StockEntryService> _logger;
    private readonly IStringLocalizer<StockEntryService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public StockEntryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IInventoryService inventoryService,
        ILogger<StockEntryService> logger,
        IStringLocalizer<StockEntryService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<StockEntryDto>> CreateStockEntryAsync(
        CreateStockEntryDto dto,
        CancellationToken ct = default)
    {
        if (dto.Items.Count == 0)
            return Result<StockEntryDto>.Failure(_localizer["At least one product is required"]);

        try
        {
            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;
            var entryDate = dto.EntryDate ?? currentTime;

            // Process inventory movements for each item
            var movementResults = new List<(CreateStockEntryItemDto Item, long MovementId)>();
            var errors = new List<string>();

            foreach (var item in dto.Items)
            {
                var movementDto = new CreateInventoryMovementDto
                {
                    MovementType = dto.MovementType,
                    MovementSubType = dto.MovementSubType,
                    LocationId = dto.LocationId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Reason = dto.Reason,
                    Document = dto.DocumentNumber,
                    Reference = dto.Reference,
                    MovementDate = entryDate
                };

                var result = await _inventoryService.CreateMovementAsync(movementDto, ct);
                if (!result.Success)
                {
                    errors.Add($"[Product {item.ProductId}] {result.Message}");
                }
                else
                {
                    movementResults.Add((item, result.Movement!.Id));
                }
            }

            if (errors.Count > 0)
                return Result<StockEntryDto>.Failure(string.Join("; ", errors));

            // All movements succeeded — persist the StockEntry header and items
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(ct);

                var stockEntry = new StockEntry
                {
                    MovementType = dto.MovementType,
                    MovementSubType = dto.MovementSubType,
                    LocationId = dto.LocationId,
                    SupplierId = dto.SupplierId,
                    SupplierName = dto.SupplierName,
                    DocumentNumber = dto.DocumentNumber,
                    Reference = dto.Reference,
                    Reason = dto.Reason,
                    EntryDate = entryDate,
                    AttachmentFileName = dto.AttachmentFileName,
                    AttachmentMimeType = dto.AttachmentMimeType,
                    AttachmentData = dto.AttachmentData,
                    CreatedBy = currentUser,
                    CreatedAt = currentTime,
                    ModifiedBy = currentUser,
                    ModifiedAt = currentTime
                };

                context.StockEntries.Add(stockEntry);
                await context.SaveChangesAsync(ct);

                var itemResults = new List<StockEntryItemResultDto>();

                foreach (var (item, movementId) in movementResults)
                {
                    var entryItem = new StockEntryItem
                    {
                        StockEntryId = stockEntry.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        InventoryMovementId = movementId,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    };
                    context.StockEntryItems.Add(entryItem);

                    // Link the movement back to this stock entry
                    var movement = await context.InventoryMovements.FindAsync([movementId], ct);
                    if (movement != null)
                    {
                        movement.StockEntryId = stockEntry.Id;
                        movement.ModifiedBy = currentUser;
                        movement.ModifiedAt = currentTime;
                    }

                    itemResults.Add(new StockEntryItemResultDto
                    {
                        ProductId = item.ProductId,
                        ProductName = string.Empty,
                        ProductCode = string.Empty,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        InventoryMovementId = movementId,
                        Success = true
                    });
                }

                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var locationName = (await context.Locations.FindAsync([dto.LocationId], ct))?.Name ?? string.Empty;

                var resultDto = new StockEntryDto
                {
                    Id = stockEntry.Id,
                    MovementType = stockEntry.MovementType,
                    MovementSubType = stockEntry.MovementSubType,
                    LocationId = stockEntry.LocationId,
                    LocationName = locationName,
                    SupplierId = stockEntry.SupplierId,
                    SupplierName = stockEntry.SupplierName,
                    DocumentNumber = stockEntry.DocumentNumber,
                    Reference = stockEntry.Reference,
                    Reason = stockEntry.Reason,
                    EntryDate = stockEntry.EntryDate,
                    AttachmentFileName = stockEntry.AttachmentFileName,
                    AttachmentMimeType = stockEntry.AttachmentMimeType,
                    Items = itemResults
                };

                return Result<StockEntryDto>.Success(resultDto);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock entry");
            return Result<StockEntryDto>.Failure(_localizer["Error creating stock entry"]);
        }
    }
}
