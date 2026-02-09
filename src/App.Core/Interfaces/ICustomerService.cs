using App.Core.DTOs.Customer;

namespace App.Core.Interfaces;

public interface ICustomerService
{
    /// <summary>
    /// Gets a paginated list of customers
    /// </summary>
    Task<(int TotalCount, IList<CustomerDto> Items)> GetCustomersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? countryCode = null,
        bool? isActive = null);

    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    Task<CustomerDto?> GetCustomerByIdAsync(long id);

    /// <summary>
    /// Gets a customer by tax ID and country code
    /// </summary>
    Task<CustomerDto?> GetCustomerByTaxIdAsync(string taxId, string countryCode);

    /// <summary>
    /// Creates a new customer
    /// </summary>
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createDto);

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerDto updateDto);

    /// <summary>
    /// Soft deletes a customer
    /// </summary>
    Task<bool> DeleteCustomerAsync(long id);

    /// <summary>
    /// Gets the public customer (general public)
    /// </summary>
    Task<CustomerDto?> GetPublicCustomerAsync();
}