using AutoMapper;

using App.Core.DTOs.Customer;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Services.Seeders;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Customers;

public class CustomerService : ICustomerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;
    private readonly IStringLocalizer<CustomerService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public CustomerService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CustomerService> logger,
        IStringLocalizer<CustomerService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<(int TotalCount, IList<CustomerDto> Items)> GetCustomersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? countryCode = null,
        bool? isActive = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Customer> query = _context.Customers
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    (x.TaxId != null && x.TaxId.Contains(searchString)) ||
                    (x.Email != null && x.Email.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                query = query.Where(x => x.CountryCode == countryCode);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and mapping
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<CustomerDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            throw;
        }
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(long id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by id {Id}", id);
            throw;
        }
    }

    public async Task<CustomerDto?> GetCustomerByTaxIdAsync(string taxId, string countryCode)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TaxId == taxId &&
                    x.CountryCode == countryCode);

            return customer != null ? _mapper.Map<CustomerDto>(customer) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by tax id {TaxId} and country {CountryCode}",
                taxId, countryCode);
            throw;
        }
    }

    public async Task<CustomerDto?> GetPublicCustomerAsync()
    {
        try
        {
            return await GetCustomerByIdAsync(CustomerSeeder.PUBLIC_CUSTOMER_ID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public customer");
            throw;
        }
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Check if customer with same tax ID already exists
            if (!string.IsNullOrEmpty(createDto.TaxId))
            {
                var exists = await _context.Customers
                    .AnyAsync(x =>
                        x.TaxId == createDto.TaxId &&
                        x.CountryCode == createDto.CountryCode);

                if (exists)
                {
                    throw new InvalidOperationException(
                        _localizer["Customer with tax ID {0} already exists", createDto.TaxId]);
                }
            }

            var customer = _mapper.Map<Customer>(createDto);

            // Público General (XAXX010101000) is covered by global invoices — never auto-invoice
            if (string.Equals(customer.TaxId?.Trim(), "XAXX010101000", StringComparison.OrdinalIgnoreCase))
            {
                customer.AutoInvoice = false;
                customer.SendInvoiceEmail = false;
            }

            // Compute fiscal readiness flag
            customer.HasFiscalData = customer.CountryCode == "MX"
                && !string.IsNullOrEmpty(customer.TaxId)
                && !string.IsNullOrEmpty(customer.PostalCode)
                && !string.IsNullOrEmpty(customer.FiscalRegime);

            // Set audit fields
            customer.CreatedBy = _currentUserService.FullName ?? "Unknow";
            customer.CreatedAt = _dateTime.Now;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerDto>(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw;
        }
    }

    public async Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerDto updateDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
            {
                throw new InvalidOperationException(
                    _localizer["Customer not found with ID {0}", id]);
            }

            // Check if tax ID is being changed and if new one already exists
            if (!string.IsNullOrEmpty(updateDto.TaxId) &&
                updateDto.TaxId != customer.TaxId)
            {
                var exists = await _context.Customers
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.TaxId == updateDto.TaxId &&
                        x.CountryCode == updateDto.CountryCode);

                if (exists)
                {
                    throw new InvalidOperationException(
                        _localizer["Customer with tax ID {0} already exists", updateDto.TaxId]);
                }
            }

            // Update properties
            _mapper.Map(updateDto, customer);

            // Público General (XAXX010101000) is covered by global invoices — never auto-invoice
            if (string.Equals(customer.TaxId?.Trim(), "XAXX010101000", StringComparison.OrdinalIgnoreCase))
            {
                customer.AutoInvoice = false;
                customer.SendInvoiceEmail = false;
            }

            // Recompute fiscal readiness flag after update
            customer.HasFiscalData = customer.CountryCode == "MX"
                && !string.IsNullOrEmpty(customer.TaxId)
                && !string.IsNullOrEmpty(customer.PostalCode)
                && !string.IsNullOrEmpty(customer.FiscalRegime);

            // Update audit fields
            customer.ModifiedBy = _currentUserService.UserId;
            customer.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerDto>(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAsync(long id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
            {
                return false;
            }

            // No permitir eliminar al cliente de público general
            if (id == CustomerSeeder.PUBLIC_CUSTOMER_ID)
            {
                throw new InvalidOperationException(
                    _localizer["Cannot delete the public general customer"]);
            }

            // Check if customer has related records
            var hasRelatedRecords = await _context.Sales
                .AnyAsync(x => x.CustomerId == id);

            if (hasRelatedRecords)
            {
                throw new InvalidOperationException(
                    _localizer["Cannot delete customer because it has related records"]);
            }

            customer.DeletedBy = _currentUserService.FullName ?? "Unknown";
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            throw;
        }
    }
}