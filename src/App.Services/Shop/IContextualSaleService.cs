using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;

namespace App.Services.Shop;

/// <summary>
/// Extends ISaleService with a context-aware overload for sale creation.
/// Use this interface (instead of ISaleService) in any service that manages
/// its own DbContext + transaction, so that the sale participates in the
/// same atomic operation. If the outer transaction rolls back, the sale rolls back too.
///
/// Usage rule:
///   - Inside a transaction  → inject IContextualSaleService, pass your context
///   - Standalone operation  → inject ISaleService, no context needed
/// </summary>
public interface IContextualSaleService : ISaleService
{
    Task<Result<SaleDto>> CreateSaleAsync(
        CreateSaleDto createDto,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default);
}
