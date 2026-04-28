using App.Core.Constants;
using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces.Shop;

public interface ISaleService
{
    /// <summary>
    /// Gets a paginated list of sales with various filters
    /// </summary>
    Task<(int TotalCount, IList<SaleDto> Items)> GetSalesAsync(
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
        bool sortDescending = true);

    /// <summary>
    /// Gets a sale by ID with all details
    /// </summary>
    Task<SaleDto?> GetSaleByIdAsync(long id);

    /// <summary>
    /// Creates a new sale with inventory management
    /// </summary>
    Task<Result<SaleDto>> CreateSaleAsync(CreateSaleDto createDto);

    /// <summary>
    /// Updates an existing sale's status and payment details
    /// </summary>
    Task<Result<SaleDto>> UpdateSaleAsync(long id, UpdateSaleDto updateDto);

    /// <summary>
    /// Cancels a sale and returns inventory
    /// </summary>
    Task<Result<bool>> CancelSaleAsync(long id, string reason);

    /// <summary>
    /// Validates if a discount can be applied based on sale type and settings
    /// </summary>
    Task<Result<bool>> ValidateDiscountAsync(
        decimal discountPercentage,
        string? authorizedBy = null);

    /// <summary>
    /// Returns cancellation eligibility for a batch of sales.
    /// A sale is blocked when it has an active individual CFDI or belongs to a Stamped global invoice.
    /// </summary>
    Task<Result<Dictionary<long, SaleCancellationStatusDto>>> GetCancellationStatusAsync(IEnumerable<long> saleIds);
}