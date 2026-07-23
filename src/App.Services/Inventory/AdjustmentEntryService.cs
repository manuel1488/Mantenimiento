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

public class AdjustmentEntryService : IAdjustmentEntryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<AdjustmentEntryService> _logger;
    private readonly IStringLocalizer<AdjustmentEntryService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public AdjustmentEntryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IInventoryService inventoryService,
        ILogger<AdjustmentEntryService> logger,
        IStringLocalizer<AdjustmentEntryService> localizer,
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

    public async Task<Result<AdjustmentEntryDto>> CreateAdjustmentEntryAsync(
        CreateAdjustmentEntryDto dto,
        CancellationToken ct = default)
    {
        if (dto.Items.Count == 0)
            return Result<AdjustmentEntryDto>.Failure(_localizer["At least one product is required"]);

        try
        {
            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;
            var adjustmentDate = dto.AdjustmentDate ?? currentTime;

            // Process inventory adjustments for each item
            var movementResults = new List<(CreateAdjustmentEntryItemDto Item, long MovementId, InventoryAlertInfo? Alert)>();
            var errors = new List<string>();

            foreach (var item in dto.Items)
            {
                var adjustmentDto = new CreateInventoryAdjustmentDto
                {
                    ProductId = item.ProductId,
                    LocationId = dto.LocationId,
                    NewQuantity = item.NewQuantity,
                    AdjustmentType = dto.AdjustmentType,
                    Reason = dto.Reason,
                    Reference = dto.Reference,
                    AdjustmentDate = adjustmentDate
                };

                var result = await _inventoryService.CreateInventoryAdjustmentAsync(adjustmentDto, ct);
                if (!result.Success)
                {
                    errors.Add($"[Product {item.ProductId}] {result.Message}");
                }
                else
                {
                    movementResults.Add((item, result.Movement!.Id, result.Alert));
                }
            }

            if (errors.Count > 0)
                return Result<AdjustmentEntryDto>.Failure(string.Join("; ", errors));

            // All movements succeeded — persist the AdjustmentEntry header and items
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(ct);

                var entry = new AdjustmentEntry
                {
                    AdjustmentType = dto.AdjustmentType,
                    LocationId = dto.LocationId,
                    Reference = dto.Reference,
                    Reason = dto.Reason,
                    AdjustmentDate = adjustmentDate,
                    CreatedBy = currentUser,
                    CreatedAt = currentTime,
                    ModifiedBy = currentUser,
                    ModifiedAt = currentTime
                };

                context.AdjustmentEntries.Add(entry);
                await context.SaveChangesAsync(ct);

                var itemResults = new List<AdjustmentEntryItemResultDto>();

                foreach (var (item, movementId, alert) in movementResults)
                {
                    var movement = await context.InventoryMovements.FindAsync([movementId], ct);
                    var previousQuantity = movement?.PreviousBalance ?? 0;

                    var entryItem = new AdjustmentEntryItem
                    {
                        AdjustmentEntryId = entry.Id,
                        ProductId = item.ProductId,
                        NewQuantity = item.NewQuantity,
                        PreviousQuantity = previousQuantity,
                        InventoryMovementId = movementId,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    };
                    context.AdjustmentEntryItems.Add(entryItem);

                    if (movement != null)
                    {
                        movement.AdjustmentEntryId = entry.Id;
                        movement.ModifiedBy = currentUser;
                        movement.ModifiedAt = currentTime;
                    }

                    itemResults.Add(new AdjustmentEntryItemResultDto
                    {
                        ProductId = item.ProductId,
                        ProductName = string.Empty,
                        ProductCode = string.Empty,
                        NewQuantity = item.NewQuantity,
                        PreviousQuantity = previousQuantity,
                        InventoryMovementId = movementId,
                        Success = true,
                        AlertType = alert?.AlertType,
                        AlertCurrentStock = alert?.CurrentStock,
                        AlertThreshold = alert?.Threshold
                    });
                }

                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var locationName = (await context.Locations.FindAsync([dto.LocationId], ct))?.Name ?? string.Empty;

                var resultDto = new AdjustmentEntryDto
                {
                    Id = entry.Id,
                    AdjustmentType = entry.AdjustmentType,
                    LocationId = entry.LocationId,
                    LocationName = locationName,
                    Reference = entry.Reference,
                    Reason = entry.Reason,
                    AdjustmentDate = entry.AdjustmentDate,
                    Items = itemResults
                };

                return Result<AdjustmentEntryDto>.Success(resultDto);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating adjustment entry");
            return Result<AdjustmentEntryDto>.Failure(_localizer["Error creating adjustment entry"]);
        }
    }
}
