using App.Core.DTOs.Customer;

namespace App.Core.Interfaces;

public interface ICustomerService
{
    /// <summary>Gets a paginated list of customers.</summary>
    Task<(int TotalCount, IList<CustomerDto> Items)> GetCustomersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? countryCode = null,
        bool? isActive = null);

    /// <summary>Gets a customer by ID (includes FiscalProfile).</summary>
    Task<CustomerDto?> GetCustomerByIdAsync(long id);

    /// <summary>Gets a customer by tax ID and country code.</summary>
    Task<CustomerDto?> GetCustomerByTaxIdAsync(string taxId, string countryCode);

    /// <summary>Creates a new customer. Optionally creates a fiscal profile in the same call.</summary>
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createDto);

    /// <summary>Updates an existing customer's commercial data. Optionally upserts fiscal profile.</summary>
    Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerDto updateDto);

    /// <summary>
    /// Creates or updates the fiscal profile for a customer.
    /// Passing null for fiscalProfileDto removes the fiscal profile.
    /// </summary>
    Task<CustomerDto> UpsertFiscalProfileAsync(long customerId, UpsertCustomerFiscalProfileDto? fiscalProfileDto);

    /// <summary>Removes the fiscal profile from a customer.</summary>
    Task<CustomerDto> RemoveFiscalProfileAsync(long customerId);

    /// <summary>Soft deletes a customer.</summary>
    Task<bool> DeleteCustomerAsync(long id);

    /// <summary>Gets the public customer (general public).</summary>
    Task<CustomerDto?> GetPublicCustomerAsync();
}
