using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Constants;
using App.Core.DTOs.Settings;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Inventory;
using App.Services.Settings;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Integration tests for SaleService.GetCancellationStatusAsync.
///
/// The method merges two EF queries (MexicoInvoices + GlobalInvoiceSales) to decide
/// whether each sale in a page is eligible for cancellation. These tests use EF InMemory
/// to verify the WHERE clauses, not mock them away.
/// </summary>
[TestFixture]
[Category("Integration")]
public class SaleCancellationStatusTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Edge cases
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCancellationStatus_EmptyInput_ReturnsEmptyDictionary()
    {
        var result = await BuildService().GetCancellationStatusAsync([]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
    public async Task GetCancellationStatus_SaleWithNoInvoice_CanCancel()
    {
        var sale = await SeedSaleAsync();

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        var status = result.Value![sale.Id];
        Assert.That(status.CanCancel, Is.True);
        Assert.That(status.BlockedByInvoice, Is.False);
        Assert.That(status.BlockedByGlobalInvoice, Is.False);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Individual invoice blocking
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCancellationStatus_SaleWithActiveInvoice_BlockedByInvoice()
    {
        var sale = await SeedSaleAsync();
        await SeedMexicoInvoiceAsync(sale.Id, "Stamped");

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        var status = result.Value![sale.Id];
        Assert.That(status.CanCancel, Is.False);
        Assert.That(status.BlockedByInvoice, Is.True);
        Assert.That(status.BlockedByGlobalInvoice, Is.False);
    }

    [Test]
    public async Task GetCancellationStatus_SaleWithCancelledInvoice_CanCancel()
    {
        var sale = await SeedSaleAsync();
        await SeedMexicoInvoiceAsync(sale.Id, "Cancelled");

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value![sale.Id].CanCancel, Is.True);
    }

    [Test]
    public async Task GetCancellationStatus_SaleWithStampErrorInvoice_CanCancel()
    {
        var sale = await SeedSaleAsync();
        await SeedMexicoInvoiceAsync(sale.Id, "StampError");

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value![sale.Id].CanCancel, Is.True);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Global invoice blocking
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCancellationStatus_SaleInStampedGlobalInvoice_BlockedByGlobal()
    {
        var sale = await SeedSaleAsync();
        var globalInvoice = await SeedGlobalInvoiceAsync(GlobalInvoiceStatus.Stamped);
        await SeedGlobalInvoiceSaleAsync(globalInvoice.Id, sale.Id);

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        var status = result.Value![sale.Id];
        Assert.That(status.CanCancel, Is.False);
        Assert.That(status.BlockedByGlobalInvoice, Is.True);
        Assert.That(status.GlobalInvoiceId, Is.EqualTo(globalInvoice.Id));
        Assert.That(status.BlockedByInvoice, Is.False);
    }

    [Test]
    public async Task GetCancellationStatus_SaleInCancelledGlobalInvoice_CanCancel()
    {
        var sale = await SeedSaleAsync();
        var globalInvoice = await SeedGlobalInvoiceAsync(GlobalInvoiceStatus.Cancelled);
        await SeedGlobalInvoiceSaleAsync(globalInvoice.Id, sale.Id);

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value![sale.Id].CanCancel, Is.True);
    }

    [Test]
    public async Task GetCancellationStatus_SaleInDraftGlobalInvoice_CanCancel()
    {
        var sale = await SeedSaleAsync();
        var globalInvoice = await SeedGlobalInvoiceAsync(GlobalInvoiceStatus.Draft);
        await SeedGlobalInvoiceSaleAsync(globalInvoice.Id, sale.Id);

        var result = await BuildService().GetCancellationStatusAsync([sale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value![sale.Id].CanCancel, Is.True);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Batch / mixed scenarios
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCancellationStatus_MixedBatch_EachSaleGetsCorrectStatus()
    {
        var freeSale = await SeedSaleAsync();
        var invoicedSale = await SeedSaleAsync();
        var globalSale = await SeedSaleAsync();

        await SeedMexicoInvoiceAsync(invoicedSale.Id, "Stamped");
        var gi = await SeedGlobalInvoiceAsync(GlobalInvoiceStatus.Stamped);
        await SeedGlobalInvoiceSaleAsync(gi.Id, globalSale.Id);

        var result = await BuildService().GetCancellationStatusAsync(
            [freeSale.Id, invoicedSale.Id, globalSale.Id]);

        Assert.That(result.IsSuccess, Is.True);
        var map = result.Value!;

        Assert.That(map[freeSale.Id].CanCancel, Is.True, "free sale should be cancellable");

        Assert.That(map[invoicedSale.Id].CanCancel, Is.False);
        Assert.That(map[invoicedSale.Id].BlockedByInvoice, Is.True);

        Assert.That(map[globalSale.Id].CanCancel, Is.False);
        Assert.That(map[globalSale.Id].BlockedByGlobalInvoice, Is.True);
        Assert.That(map[globalSale.Id].GlobalInvoiceId, Is.EqualTo(gi.Id));
    }

    [Test]
    public async Task GetCancellationStatus_OnlyRequestedSalesReturned()
    {
        var target = await SeedSaleAsync();
        var other = await SeedSaleAsync();
        await SeedMexicoInvoiceAsync(other.Id, "Stamped"); // noise — different sale

        var result = await BuildService().GetCancellationStatusAsync([target.Id]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.ContainsKey(other.Id), Is.False,
            "Sales outside the requested IDs must not appear in the result");
        Assert.That(result.Value![target.Id].CanCancel, Is.True);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private SaleService BuildService()
    {
        var localizer = new Mock<IStringLocalizer<SaleService>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));
        localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        return new SaleService(
            contextFactory: new TestDbContextFactory(_dbOptions),
            mapper: new Mock<IMapper>().Object,
            logger: NullLogger<SaleService>.Instance,
            localizer: localizer.Object,
            currentUserService: new Mock<ICurrentUserService>().Object,
            dateTime: new Mock<IDateTime>().Object,
            discountSettingsService: new Mock<IDiscountSettingsService>().Object,
            discountAuthorizerService: new Mock<IDiscountAuthorizerService>().Object,
            inventoryService: new Mock<IContextualInventoryService>().Object,
            taxRateService: new Mock<ITaxRateService>().Object,
            companySettingsService: new Mock<ICompanySettingsService>().Object,
            taxSettingsService: new Mock<ITaxSettingsService>().Object,
            productPartialSurchargeService: new Mock<IProductPartialSurchargeService>().Object,
            roundingSettingsService: new Mock<IRoundingSettingsService>().Object,
            cashRegisterService: new Mock<ICashRegisterService>().Object,
            pricingService: new Mock<IPricingCalculationService>().Object);
    }

    private async Task<Sale> SeedSaleAsync()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var customer = await EnsureCustomerAsync(ctx);
        var sale = new Sale
        {
            CustomerId = customer.Id,
            SaleType = SaleType.Public,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            SaleDate = DateTime.UtcNow,
            Subtotal = 100m,
            TaxAmount = 16m,
            Total = 116m,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.Sales.Add(sale);
        await ctx.SaveChangesAsync();
        return sale;
    }

    private async Task<MexicoInvoice> SeedMexicoInvoiceAsync(long saleId, string status)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var invoice = new MexicoInvoice
        {
            SaleId = saleId,
            Status = status,
            CfdiUse = "G03",
            PaymentForm = "01",
            PaymentMethod = "PUE",
            CustomerRfc = "XAXX010101000",
            CustomerLegalName = "PÚBLICO EN GENERAL",
            CustomerPostalCode = "64000",
            CustomerFiscalRegime = "616",
            IssuerRfc = "TEST010101TST",
            IssuerLegalName = "EMPRESA TEST SA DE CV",
            IssuerFiscalRegime = "601",
            IssuerPostalCode = "64000",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.MexicoInvoices.Add(invoice);
        await ctx.SaveChangesAsync();
        return invoice;
    }

    private async Task<GlobalInvoice> SeedGlobalInvoiceAsync(GlobalInvoiceStatus status)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var invoice = new GlobalInvoice
        {
            Serie = "G",
            Folio = 1,
            Periodicity = GlobalInvoicePeriodicity.Monthly,
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc),
            PeriodMonth = "03",
            PeriodYear = 2026,
            PaymentForm = "01",
            SaleCount = 1,
            Subtotal = 100m,
            DiscountAmount = 0m,
            TaxAmount = 16m,
            Total = 116m,
            Status = status,
            IssuerRfc = "TEST010101TST",
            IssuerLegalName = "EMPRESA TEST",
            IssuerFiscalRegime = "601",
            IssuerPostalCode = "64000",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.GlobalInvoices.Add(invoice);
        await ctx.SaveChangesAsync();
        return invoice;
    }

    private async Task SeedGlobalInvoiceSaleAsync(long globalInvoiceId, long saleId)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.GlobalInvoiceSales.Add(new GlobalInvoiceSale
        {
            GlobalInvoiceId = globalInvoiceId,
            SaleId = saleId
        });
        await ctx.SaveChangesAsync();
    }

    private static async Task<Customer> EnsureCustomerAsync(ApplicationDbContext ctx)
    {
        var existing = ctx.Customers.FirstOrDefault();
        if (existing != null) return existing;

        var customer = new Customer
        {
            Name = "Test Customer",
            Email = "test@test.com",
            CountryCode = "MX",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        return customer;
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken _ = default)
            => Task.FromResult(new ApplicationDbContext(options));
    }
}
