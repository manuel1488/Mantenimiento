using App.Core.Constants;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class CustomerSeeder : ICustomerSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CustomerSeeder> _logger;
    private readonly IDateTime _dateTime;
    private readonly string _systemUser = "System";

    // ID constante para el cliente público general
    public const long PUBLIC_CUSTOMER_ID = 1;

    public CustomerSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CustomerSeeder> logger,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedPublicCustomerAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding public customer");
            throw;
        }
    }

    private async Task SeedPublicCustomerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Verificar si ya existe el cliente público general
        var publicCustomerExists = await context.Customers
            .AnyAsync(c => c.Id == PUBLIC_CUSTOMER_ID);

        if (!publicCustomerExists)
        {
            // Crear cliente para público general
            var publicCustomer = new Customer
            {
                Id = PUBLIC_CUSTOMER_ID,
                Name = "Público General",
                LegalName = "Público General",
                CountryCode = CountryCodes.Mexico, // Por defecto México
                CreatedBy = _systemUser,
                CreatedAt = _dateTime.Now
            };

            context.Customers.Add(publicCustomer);
            await context.SaveChangesAsync();

            _logger.LogInformation("Public general customer seeded successfully");
        }
    }
}