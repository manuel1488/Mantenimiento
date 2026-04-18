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
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Customer> query = context.Customers
                .AsNoTracking()
                .Include(c => c.FiscalProfile);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    (x.Email != null && x.Email.Contains(searchString)) ||
                    (x.FiscalProfile != null && x.FiscalProfile.TaxId.Contains(searchString)) ||
                    (x.FiscalProfile != null && x.FiscalProfile.LegalName.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                query = query.Where(x => x.CountryCode == countryCode);
            }

            var totalCount = await query.CountAsync();

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
            await using var context = await _contextFactory.CreateDbContextAsync();

            var customer = await context.Customers
                .AsNoTracking()
                .Include(c => c.FiscalProfile)
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
            await using var context = await _contextFactory.CreateDbContextAsync();

            var customer = await context.Customers
                .AsNoTracking()
                .Include(c => c.FiscalProfile)
                .FirstOrDefaultAsync(x =>
                    x.CountryCode == countryCode &&
                    x.FiscalProfile != null &&
                    x.FiscalProfile.TaxId == taxId);

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
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Validate fiscal profile if provided
            if (createDto.FiscalProfile != null)
            {
                await ValidateFiscalProfileTaxIdAsync(context, createDto.FiscalProfile.TaxId,
                    createDto.CountryCode, excludeCustomerId: null);
            }

            var customer = _mapper.Map<Customer>(createDto);

            var currentUser = _currentUserService.FullName ?? "Unknown";
            var now = _dateTime.Now;
            customer.CreatedBy = currentUser;
            customer.CreatedAt = now;

            if (createDto.FiscalProfile != null)
            {
                var profile = _mapper.Map<CustomerFiscalProfile>(createDto.FiscalProfile);
                ApplyPublicGeneralRules(profile);
                profile.CreatedBy = currentUser;
                profile.CreatedAt = now;
                customer.FiscalProfile = profile;
            }

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

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
            await using var context = await _contextFactory.CreateDbContextAsync();

            var customer = await context.Customers
                .Include(c => c.FiscalProfile)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                throw new InvalidOperationException(_localizer["Customer not found with ID {0}", id]);

            // Validate fiscal TaxId uniqueness if being changed
            if (updateDto.FiscalProfile != null)
            {
                var currentTaxId = customer.FiscalProfile?.TaxId;
                if (updateDto.FiscalProfile.TaxId != currentTaxId)
                {
                    await ValidateFiscalProfileTaxIdAsync(context, updateDto.FiscalProfile.TaxId,
                        updateDto.CountryCode, excludeCustomerId: id);
                }
            }

            // Update commercial fields
            _mapper.Map(updateDto, customer);

            var currentUser = _currentUserService.UserId ?? "Unknown";
            var now = _dateTime.Now;
            customer.ModifiedBy = currentUser;
            customer.ModifiedAt = now;

            // Handle fiscal profile upsert
            if (updateDto.FiscalProfile != null)
            {
                if (customer.FiscalProfile == null)
                {
                    var profile = _mapper.Map<CustomerFiscalProfile>(updateDto.FiscalProfile);
                    profile.CustomerId = customer.Id;
                    ApplyPublicGeneralRules(profile);
                    profile.CreatedBy = currentUser;
                    profile.CreatedAt = now;
                    customer.FiscalProfile = profile;
                }
                else
                {
                    _mapper.Map(updateDto.FiscalProfile, customer.FiscalProfile);
                    ApplyPublicGeneralRules(customer.FiscalProfile);
                    customer.FiscalProfile.ModifiedBy = currentUser;
                    customer.FiscalProfile.ModifiedAt = now;
                }
            }

            await context.SaveChangesAsync();

            return _mapper.Map<CustomerDto>(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", id);
            throw;
        }
    }

    public async Task<CustomerDto> UpsertFiscalProfileAsync(long customerId, UpsertCustomerFiscalProfileDto? fiscalProfileDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var customer = await context.Customers
                .Include(c => c.FiscalProfile)
                .FirstOrDefaultAsync(x => x.Id == customerId);

            if (customer == null)
                throw new InvalidOperationException(_localizer["Customer not found with ID {0}", customerId]);

            var currentUser = _currentUserService.UserId ?? "Unknown";
            var now = _dateTime.Now;

            if (fiscalProfileDto == null)
            {
                if (customer.FiscalProfile != null)
                {
                    context.CustomerFiscalProfiles.Remove(customer.FiscalProfile);
                    customer.FiscalProfile = null;
                }
            }
            else
            {
                var currentTaxId = customer.FiscalProfile?.TaxId;
                if (fiscalProfileDto.TaxId != currentTaxId)
                {
                    await ValidateFiscalProfileTaxIdAsync(context, fiscalProfileDto.TaxId,
                        customer.CountryCode, excludeCustomerId: customerId);
                }

                if (customer.FiscalProfile == null)
                {
                    var profile = _mapper.Map<CustomerFiscalProfile>(fiscalProfileDto);
                    profile.CustomerId = customerId;
                    ApplyPublicGeneralRules(profile);
                    profile.CreatedBy = currentUser;
                    profile.CreatedAt = now;
                    customer.FiscalProfile = profile;
                }
                else
                {
                    _mapper.Map(fiscalProfileDto, customer.FiscalProfile);
                    ApplyPublicGeneralRules(customer.FiscalProfile);
                    customer.FiscalProfile.ModifiedBy = currentUser;
                    customer.FiscalProfile.ModifiedAt = now;
                }
            }

            customer.ModifiedBy = currentUser;
            customer.ModifiedAt = now;

            await context.SaveChangesAsync();

            return _mapper.Map<CustomerDto>(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting fiscal profile for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<CustomerDto> RemoveFiscalProfileAsync(long customerId)
    {
        return await UpsertFiscalProfileAsync(customerId, null);
    }

    public async Task<bool> DeleteCustomerAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var customer = await context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                return false;

            if (id == CustomerSeeder.PUBLIC_CUSTOMER_ID)
                throw new InvalidOperationException(_localizer["Cannot delete the public general customer"]);

            var hasRelatedRecords = await context.Sales.AnyAsync(x => x.CustomerId == id);
            if (hasRelatedRecords)
                throw new InvalidOperationException(_localizer["Cannot delete customer because it has related records"]);

            customer.DeletedBy = _currentUserService.FullName ?? "Unknown";
            context.Customers.Remove(customer);
            await context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            throw;
        }
    }

    // --- Private helpers ---

    /// <summary>Validates that TaxId is not already used by another customer in the same country.</summary>
    private static async Task ValidateFiscalProfileTaxIdAsync(
        ApplicationDbContext context,
        string taxId,
        string countryCode,
        long? excludeCustomerId)
    {
        // Block XAXX010101000 — reserved for Público General global invoices
        if (string.Equals(taxId.Trim(), "XAXX010101000", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "RFC XAXX010101000 is reserved for Público General and cannot be assigned to a customer fiscal profile.");

        var query = context.CustomerFiscalProfiles
            .Where(fp => fp.TaxId == taxId && fp.Customer.CountryCode == countryCode);

        if (excludeCustomerId.HasValue)
            query = query.Where(fp => fp.CustomerId != excludeCustomerId.Value);

        var exists = await query.AnyAsync();
        if (exists)
            throw new InvalidOperationException($"A customer with tax ID {taxId} already exists for country {countryCode}.");
    }

    /// <summary>Enforces that Público General (Id=1) can never have AutoInvoice or SendInvoiceEmail enabled.</summary>
    private static void ApplyPublicGeneralRules(CustomerFiscalProfile profile)
    {
        if (string.Equals(profile.TaxId?.Trim(), "XAXX010101000", StringComparison.OrdinalIgnoreCase))
        {
            profile.AutoInvoice = false;
            profile.SendInvoiceEmail = false;
        }
    }
}
