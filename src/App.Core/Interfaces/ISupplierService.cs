using App.Core.Common;
using App.Core.DTOs.Shop;

namespace App.Core.Interfaces;

public interface ISupplierService
{
    /// <summary>
    /// Gets a paginated list of suppliers
    /// </summary>
    Task<(int TotalCount, IList<SupplierDto> Items)> GetSuppliersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null);

    /// <summary>
    /// Gets all active suppliers (for dropdowns)
    /// </summary>
    Task<IList<SupplierDto>> GetActiveSuppliersAsync();

    /// <summary>
    /// Gets a supplier by ID
    /// </summary>
    Task<SupplierDto?> GetSupplierByIdAsync(long id);

    /// <summary>
    /// Creates a new supplier
    /// </summary>
    Task<Result<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto);

    /// <summary>
    /// Updates an existing supplier
    /// </summary>
    Task<Result<SupplierDto>> UpdateSupplierAsync(long id, UpdateSupplierDto dto);

    /// <summary>
    /// Soft deletes a supplier
    /// </summary>
    Task<Result> DeleteSupplierAsync(long id);
}
