using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;

namespace App.Services.Inventory;

/// <summary>
/// Extends IInventoryService with a context-aware overload for inventory movements.
/// Use this interface (instead of IInventoryService) in any service that manages
/// its own DbContext + transaction, so that inventory movements participate in the
/// same atomic operation. If the outer transaction rolls back, the movement rolls back too.
///
/// Usage rule:
///   - Inside a transaction  → inject IContextualInventoryService, pass your context
///   - Standalone operation  → inject IInventoryService, no context needed
/// </summary>
public interface IContextualInventoryService : IInventoryService
{
    Task<MovementOperationResult> CreateMovementAsync(
        CreateInventoryMovementDto createDto,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default);
}
