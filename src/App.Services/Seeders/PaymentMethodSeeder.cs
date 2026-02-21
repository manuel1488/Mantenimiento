using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class PaymentMethodSeeder : IPaymentMethodSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<PaymentMethodSeeder> _logger;
    private const string SystemUser = "System";

    public PaymentMethodSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<PaymentMethodSeeder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (await context.PaymentMethods.AnyAsync())
                return;

            var now = DateTime.UtcNow;
            var methods = new List<PaymentMethod>
            {
                Create("Efectivo",           PaymentMethodType.Cash,     null,               "01", "attach_money",    1),
                Create("Tarjeta de Débito",  PaymentMethodType.Card,     CardSubtype.Debit,  "28", "credit_card",     2),
                Create("Tarjeta de Crédito", PaymentMethodType.Card,     CardSubtype.Credit, "04", "credit_card",     3),
                Create("Transferencia",      PaymentMethodType.Transfer, null,               "03", "account_balance", 4),
                Create("Cheque",             PaymentMethodType.Check,    null,               "02", "receipt",         5),
                Create("Otro",               PaymentMethodType.Other,    null,               "99", "payments",        6),
            };

            await context.PaymentMethods.AddRangeAsync(methods);
            await context.SaveChangesAsync();

            _logger.LogInformation("Payment methods seeded successfully ({Count} records)", methods.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding payment methods");
            throw;
        }
    }

    private static PaymentMethod Create(
        string name,
        PaymentMethodType type,
        CardSubtype? cardSubtype,
        string? mxCfdiFormCode,
        string icon,
        int sortOrder)
    {
        return new PaymentMethod
        {
            Name = name,
            Type = type,
            CardSubtype = cardSubtype,
            MxCfdiFormCode = mxCfdiFormCode,
            Icon = icon,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedBy = SystemUser,
            CreatedAt = DateTime.UtcNow
        };
    }
}
