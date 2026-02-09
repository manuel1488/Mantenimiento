using App.Core.Common;
using App.Core.DTOs.Warehouse;

namespace App.Core.Interfaces;

public interface IWarehouseService
{
    /// <summary>
    /// Gets a paginated list of warehouses
    /// </summary>
    Task<(int TotalCount, IList<WarehouseDto> Items)> GetWarehousesAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null);

    /// <summary>
    /// Gets a warehouse by ID
    /// </summary>
    Task<WarehouseDto?> GetWarehouseByIdAsync(int id);

    /// <summary>
    /// Creates a new warehouse
    /// </summary>
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createDto);

    /// <summary>
    /// Updates an existing warehouse
    /// </summary>
    Task<WarehouseDto> UpdateWarehouseAsync(int id, UpdateWarehouseDto updateDto);

    /// <summary>
    /// Soft deletes a warehouse
    /// </summary>
    Task<bool> DeleteWarehouseAsync(int id);

    /// <summary>
    /// Validates that a warehouse name is unique
    /// </summary>
    Task<bool> ValidateUniqueNameAsync(string name, int? excludeId = null);

    /// <summary>
    /// Sets a warehouse as the public sales warehouse
    /// </summary>
    Task<Result<bool>> SetPublicSalesWarehouseAsync(int warehouseId);

    /// <summary>
    /// Gets the warehouse designated for public sales
    /// </summary>
    Task<Result<WarehouseDto?>> GetPublicSalesWarehouseAsync();
}