using Moq;
using NUnit.Framework;

using App.Core.DTOs.Settings;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Constants;
using App.Services.Settings;
using App.Shared.Services;
using App.Core.Options;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Billing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace App.Services.Tests.Billing;

/// <summary>
/// Integration tests for GlobalInvoiceService query methods added alongside the detail-view feature:
///
///   GetActiveSaleToInvoiceMapAsync — saleId→globalInvoiceId map, only for Stamped invoices.
///   GetByIdAsync                   — Sales collection populated via Include(GlobalInvoiceSales).
///
/// These are integration tests (EF Core InMemory) because the logic being tested IS the
/// database query/filter — mocking DbContext would not verify the WHERE clause correctness.
/// </summary>
[TestFixture]
[Category("Integration")]
public class GlobalInvoiceDetailTests
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
    // GetActiveSaleToInvoiceMapAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetActiveSaleToInvoiceMap_NoInvoices_ReturnsEmptyDictionary()
    {
        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
    public async Task GetActiveSaleToInvoiceMap_StampedInvoiceWithSales_ReturnsSaleToInvoiceMap()
    {
        var (globalInvoiceId, saleIds) = await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Stamped, salesCount: 3);

        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(3));
        foreach (var saleId in saleIds)
        {
            Assert.That(result.Value!.ContainsKey(saleId), Is.True,
                $"SaleId {saleId} must be in the map");
            Assert.That(result.Value![saleId], Is.EqualTo(globalInvoiceId),
                $"SaleId {saleId} must map to globalInvoiceId {globalInvoiceId}");
        }
    }

    [Test]
    public async Task GetActiveSaleToInvoiceMap_CancelledInvoice_ExcludedFromMap()
    {
        // Cancelled invoices must not block re-invoicing in a new global or individual CFDI
        await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Cancelled, salesCount: 2);

        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty, "Cancelled invoices must not appear in the active map");
    }

    [Test]
    public async Task GetActiveSaleToInvoiceMap_DraftInvoice_ExcludedFromMap()
    {
        await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Draft, salesCount: 2);

        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty, "Draft invoices must not appear in the active map");
    }

    [Test]
    public async Task GetActiveSaleToInvoiceMap_StampErrorInvoice_ExcludedFromMap()
    {
        // StampError invoices do not lock their sales — the operator can retry with a new invoice
        await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.StampError, salesCount: 2);

        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty, "StampError invoices must not appear in the active map");
    }

    [Test]
    public async Task GetActiveSaleToInvoiceMap_MixedStatuses_OnlyStampedSalesIncluded()
    {
        var (stampedId, stampedSaleIds) = await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Stamped, salesCount: 2);
        await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Cancelled, salesCount: 1);
        await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Draft, salesCount: 1);

        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2), "Only the 2 Stamped sales should be in the map");
        foreach (var id in stampedSaleIds)
            Assert.That(result.Value!.ContainsKey(id), Is.True);
    }

    [Test]
    public async Task GetActiveSaleToInvoiceMap_MultipleStampedInvoices_EachSaleMapsToItsOwnInvoice()
    {
        var (invoiceA, salesA) = await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Stamped, salesCount: 2);
        var (invoiceB, salesB) = await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Stamped, salesCount: 3);

        var result = await BuildService().GetActiveSaleToInvoiceMapAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(5));

        foreach (var id in salesA)
            Assert.That(result.Value![id], Is.EqualTo(invoiceA),
                $"Sale {id} should map to invoice {invoiceA}, not {invoiceB}");
        foreach (var id in salesB)
            Assert.That(result.Value![id], Is.EqualTo(invoiceB),
                $"Sale {id} should map to invoice {invoiceB}, not {invoiceA}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetByIdAsync — Sales collection
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetById_NotFound_ReturnsFailure()
    {
        var result = await BuildService().GetByIdAsync(9999);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetById_StampedInvoice_ReturnsSalesCollection()
    {
        var (invoiceId, saleIds) = await SeedGlobalInvoiceWithSalesAsync(GlobalInvoiceStatus.Stamped, salesCount: 3);

        var result = await BuildService().GetByIdAsync(invoiceId);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.Sales, Has.Count.EqualTo(3),
            "GetByIdAsync must populate Sales via Include(GlobalInvoiceSales).ThenInclude(Sale)");

        var returnedIds = result.Value.Sales.Select(s => s.SaleId).ToHashSet();
        foreach (var id in saleIds)
            Assert.That(returnedIds.Contains(id), Is.True, $"SaleId {id} must be in the returned Sales");
    }

    [Test]
    public async Task GetById_SalesOrderedByDateAscending()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var customer = await SeedCustomerAsync(ctx);

        // Seed sales in non-chronological order
        var dates = new[]
        {
            new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 10, 08, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 20, 18, 0, 0, DateTimeKind.Utc)
        };

        var saleIds = new List<long>();
        foreach (var date in dates)
        {
            var sale = BuildSale(customer.Id, date);
            ctx.Sales.Add(sale);
            await ctx.SaveChangesAsync();
            saleIds.Add(sale.Id);
        }

        var invoice = BuildGlobalInvoice(GlobalInvoiceStatus.Stamped);
        ctx.GlobalInvoices.Add(invoice);
        await ctx.SaveChangesAsync();

        ctx.GlobalInvoiceSales.AddRange(saleIds.Select(sid =>
            new GlobalInvoiceSale { GlobalInvoiceId = invoice.Id, SaleId = sid }));
        await ctx.SaveChangesAsync();

        var result = await BuildService().GetByIdAsync(invoice.Id);

        Assert.That(result.IsSuccess, Is.True);
        var returnedDates = result.Value!.Sales.Select(s => s.SaleDate).ToList();
        Assert.That(returnedDates, Is.Ordered.Ascending,
            "Sales must be ordered by SaleDate ascending in the detail view");
    }

    [Test]
    public async Task GetById_SalesDto_AmountsMatchSaleEntity()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var customer = await SeedCustomerAsync(ctx);

        var sale = BuildSale(customer.Id, new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            subtotal: 100m, tax: 16m, total: 116m);
        ctx.Sales.Add(sale);

        var invoice = BuildGlobalInvoice(GlobalInvoiceStatus.Stamped);
        ctx.GlobalInvoices.Add(invoice);
        await ctx.SaveChangesAsync();

        ctx.GlobalInvoiceSales.Add(new GlobalInvoiceSale { GlobalInvoiceId = invoice.Id, SaleId = sale.Id });
        await ctx.SaveChangesAsync();

        var result = await BuildService().GetByIdAsync(invoice.Id);

        Assert.That(result.IsSuccess, Is.True);
        var dto = result.Value!.Sales.Single();
        Assert.That(dto.SaleId,    Is.EqualTo(sale.Id));
        Assert.That(dto.SaleDate,  Is.EqualTo(sale.SaleDate));
        Assert.That(dto.Subtotal,  Is.EqualTo(100m));
        Assert.That(dto.TaxAmount, Is.EqualTo(16m));
        Assert.That(dto.Total,     Is.EqualTo(116m));
    }

    [Test]
    public async Task GetById_HasCancellationAcuse_TrueWhenAcuseStored()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var invoice = BuildGlobalInvoice(GlobalInvoiceStatus.Cancelled);
        invoice.CancellationAcuse = "<xml>acuse</xml>";
        ctx.GlobalInvoices.Add(invoice);
        await ctx.SaveChangesAsync();

        var result = await BuildService().GetByIdAsync(invoice.Id);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.HasCancellationAcuse, Is.True,
            "HasCancellationAcuse must be true when CancellationAcuse XML is present");
    }

    [Test]
    public async Task GetById_HasCancellationAcuse_FalseWhenAcuseAbsent()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var invoice = BuildGlobalInvoice(GlobalInvoiceStatus.Stamped);
        invoice.CancellationAcuse = null;
        ctx.GlobalInvoices.Add(invoice);
        await ctx.SaveChangesAsync();

        var result = await BuildService().GetByIdAsync(invoice.Id);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.HasCancellationAcuse, Is.False,
            "HasCancellationAcuse must be false when no CancellationAcuse is stored");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private GlobalInvoiceService BuildService()
    {
        var localizer = new Mock<IStringLocalizer<GlobalInvoiceService>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));

        var taxSettings = new Mock<ITaxSettingsService>();
        taxSettings.Setup(x => x.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                CountryCode = "MX",
                TaxId = "XAXX010101000",
                BusinessName = "TEST SA",
                FiscalRegime = "601",
                PostalCode = "64000"
            });

        return new GlobalInvoiceService(
            contextFactory: new TestDbContextFactory(_dbOptions),
            xmlService: new Mock<IMexicoCfdiXmlService>().Object,
            signingService: new Mock<IMexicoCsdSigningService>().Object,
            pacService: new Mock<ISwSapienService>().Object,
            pacSettingsService: new Mock<IMexicoPacSettingsService>().Object,
            taxSettingsService: taxSettings.Object,
            taxRateService: new Mock<ITaxRateService>().Object,
            companySettingsService: new Mock<ICompanySettingsService>().Object,
            pdfService: new Mock<IPdfService>().Object,
            emailTemplateService: new Mock<IEmailTemplateService>().Object,
            fiscalCatalogService: new Mock<IMexicoFiscalCatalogService>().Object,
            currentUserService: new Mock<ICurrentUserService>().Object,
            dateTime: new Mock<IDateTime>().Object,
            applicationOptions: Options.Create(new ApplicationOptions { Name = "Test", BaseUrl = "http://localhost" }),
            localizer: localizer.Object,
            logger: NullLogger<GlobalInvoiceService>.Instance);
    }

    private async Task<(long GlobalInvoiceId, List<long> SaleIds)> SeedGlobalInvoiceWithSalesAsync(
        GlobalInvoiceStatus status, int salesCount)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var customer = await SeedCustomerAsync(ctx);

        var invoice = BuildGlobalInvoice(status);
        ctx.GlobalInvoices.Add(invoice);
        await ctx.SaveChangesAsync();

        var saleIds = new List<long>();
        for (var i = 0; i < salesCount; i++)
        {
            var sale = BuildSale(customer.Id, DateTime.UtcNow.AddMinutes(i));
            ctx.Sales.Add(sale);
            await ctx.SaveChangesAsync();
            saleIds.Add(sale.Id);
        }

        ctx.GlobalInvoiceSales.AddRange(saleIds.Select(sid => new GlobalInvoiceSale
        {
            GlobalInvoiceId = invoice.Id,
            SaleId = sid
        }));
        await ctx.SaveChangesAsync();

        return (invoice.Id, saleIds);
    }

    private static async Task<Customer> SeedCustomerAsync(ApplicationDbContext ctx)
    {
        var existing = ctx.Customers.FirstOrDefault();
        if (existing != null) return existing;

        var customer = new Customer
        {
            Name = "Público General",
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

    private static GlobalInvoice BuildGlobalInvoice(GlobalInvoiceStatus status) => new()
    {
        Serie = "G",
        Folio = 1,
        Periodicity = GlobalInvoicePeriodicity.Monthly,
        StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc),
        PeriodMonth = "03",
        PeriodYear = 2026,
        PaymentForm = "01",
        SaleCount = 0,
        Subtotal = 0m,
        DiscountAmount = 0m,
        TaxAmount = 0m,
        Total = 0m,
        Status = status,
        IssuerRfc = "TEST010101TST",
        IssuerLegalName = "EMPRESA TEST SA DE CV",
        IssuerFiscalRegime = "601",
        IssuerPostalCode = "64000",
        CreatedBy = "seed",
        CreatedAt = DateTime.UtcNow,
        ModifiedBy = "seed",
        ModifiedAt = DateTime.UtcNow
    };

    private static Sale BuildSale(long customerId, DateTime saleDate,
        decimal subtotal = 100m, decimal tax = 16m, decimal total = 116m) => new()
    {
        CustomerId = customerId,
        SaleType = SaleType.Public,
        Status = App.Core.Enums.Shop.SaleStatus.Created,
        SaleDate = saleDate,
        Subtotal = subtotal,
        TaxAmount = tax,
        Total = total,
        CreatedBy = "seed",
        CreatedAt = DateTime.UtcNow,
        ModifiedBy = "seed",
        ModifiedAt = DateTime.UtcNow
    };

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken _ = default)
            => Task.FromResult(new ApplicationDbContext(options));
    }
}
