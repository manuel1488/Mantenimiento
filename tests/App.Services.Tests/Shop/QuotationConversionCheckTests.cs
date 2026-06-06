using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Enums.Settings;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Settings;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Validates the discrepancy report surfaced to the operator before converting a
/// quotation to a sale: catalog price changes, active rounding, and tax rate changes.
/// </summary>
[TestFixture]
public class QuotationConversionCheckTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private QuotationService _service = null!;
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<ITaxRateService> _taxRateMock = null!;
    private Mock<IRoundingSettingsService> _roundingMock = null!;

    private const decimal QuotedTaxRate = 0.16m;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var localizerMock = new Mock<IStringLocalizer<QuotationService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.UserId).Returns((string?)"test-user");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto { CountryCode = "MX" });

        // Tax rate defaults to the quoted rate (no change). Tests override as needed.
        _taxRateMock = new Mock<ITaxRateService>();
        _taxRateMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(QuotedTaxRate);

        // Rounding defaults to disabled. Tests override as needed.
        _roundingMock = new Mock<IRoundingSettingsService>();
        _roundingMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoundingSettingsDto>.Success(new RoundingSettingsDto { IsEnabled = false }));

        _service = new QuotationService(
            new TestDbContextFactory(_dbOptions),
            new Mock<IMapper>().Object,
            NullLogger<QuotationService>.Instance,
            localizerMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            _taxRateMock.Object,
            companySettingsMock.Object,
            new Mock<IEmailService>().Object,
            new Mock<IEmailTemplateService>().Object,
            new Mock<IPdfService>().Object,
            new PricingCalculationService(
                _taxRateMock.Object,
                companySettingsMock.Object,
                _roundingMock.Object,
                NullLogger<PricingCalculationService>.Instance),
            new Mock<IDocumentSequenceService>().Object,
            new Mock<IQuotationSettingsService>().Object,
            _roundingMock.Object,
            new Mock<ITaxSettingsService>().Object);

        SeedBase();
    }

    private void SeedBase()
    {
        using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.UnitMeasures.Add(new UnitMeasure
        {
            Id = 1, Code = "PZA", Name = "Pieza", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        ctx.Customers.Add(new Customer
        {
            Id = 1, Name = "Test Customer", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();
    }

    private void SeedProduct(long id, decimal price, bool isPartialSaleAllowed = false, decimal content = 1)
    {
        using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Products.Add(new Product
        {
            Id = id, Code = $"P{id:D4}", Name = $"Product {id}", Brand = "Test",
            Price = price, Cost = 0, IsTaxable = true, IsActive = true,
            UnitMeasureId = 1, Content = content, IsPartialSaleAllowed = isPartialSaleAllowed,
            QuantityStep = 1, RequiresInventory = false,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();
    }

    private async Task<long> SeedQuotationAsync(long productId, decimal quotedUnitPrice)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = new Quotation
        {
            QuotationNumber = "COT-TEST-0001",
            CustomerId = 1,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = QuotationStatus.Accepted,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            Details =
            [
                new QuotationDetail
                {
                    ProductId = productId,
                    ProductName = $"Product {productId}",
                    ProductCode = $"P{productId:D4}",
                    Quantity = 2,
                    UnitPrice = quotedUnitPrice,
                    TaxRate = QuotedTaxRate,
                    CreatedBy = "seed", CreatedAt = DateTime.UtcNow
                }
            ]
        };
        ctx.Quotations.Add(quotation);
        await ctx.SaveChangesAsync();
        return quotation.Id;
    }

    // =========================================================================

    [Test]
    public async Task NoChanges_ReturnsNoWarnings()
    {
        SeedProduct(10, price: 50.00m);
        var id = await SeedQuotationAsync(productId: 10, quotedUnitPrice: 50.00m);

        var result = await _service.CheckConversionDiscrepanciesAsync(id);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.HasWarnings, Is.False);
        Assert.That(result.Value!.PriceChanges, Is.Empty);
        Assert.That(result.Value!.RoundingEnabled, Is.False);
        Assert.That(result.Value!.TaxRateChanged, Is.False);
    }

    [Test]
    public async Task CatalogPriceChanged_ReportsPriceChange()
    {
        // Quoted at 45.00, catalog now 50.00.
        SeedProduct(11, price: 50.00m);
        var id = await SeedQuotationAsync(productId: 11, quotedUnitPrice: 45.00m);

        var result = await _service.CheckConversionDiscrepanciesAsync(id);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.HasWarnings, Is.True);
        Assert.That(result.Value!.PriceChanges, Has.Count.EqualTo(1));
        var change = result.Value!.PriceChanges[0];
        Assert.That(change.QuotedPrice, Is.EqualTo(45.00m));
        Assert.That(change.CurrentPrice, Is.EqualTo(50.00m));
        Assert.That(change.ProductCode, Is.EqualTo("P0011"));
    }

    [Test]
    public async Task PartialSaleFractionalArtifact_NotFlaggedAsPriceChange()
    {
        // Partial-sale product: quoted per-fraction price 6.034483 vs catalog 6.04 / content 1.
        // The ~0.0055 difference is a 6-decimal fractional artifact, not a real price change.
        SeedProduct(12, price: 6.04m, isPartialSaleAllowed: true, content: 1);
        var id = await SeedQuotationAsync(productId: 12, quotedUnitPrice: 6.034483m);

        var result = await _service.CheckConversionDiscrepanciesAsync(id);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.PriceChanges, Is.Empty,
            "Sub-cent fractional differences must not be reported as price changes");
    }

    [Test]
    public async Task RoundingEnabled_ReportsRounding()
    {
        _roundingMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoundingSettingsDto>.Success(new RoundingSettingsDto
            {
                IsEnabled = true,
                Method = RoundingMethod.Ceiling,
                DecimalPlaces = 0
            }));

        SeedProduct(13, price: 50.00m);
        var id = await SeedQuotationAsync(productId: 13, quotedUnitPrice: 50.00m);

        var result = await _service.CheckConversionDiscrepanciesAsync(id);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.RoundingEnabled, Is.True);
        Assert.That(result.Value!.HasWarnings, Is.True);
    }

    [Test]
    public async Task TaxRateChanged_ReportsTaxChange()
    {
        // Quotation stored 16%, current effective rate is now 8%.
        _taxRateMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.08m);

        SeedProduct(14, price: 50.00m);
        var id = await SeedQuotationAsync(productId: 14, quotedUnitPrice: 50.00m);

        var result = await _service.CheckConversionDiscrepanciesAsync(id);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.TaxRateChanged, Is.True);
        Assert.That(result.Value!.QuotedTaxRate, Is.EqualTo(0.16m));
        Assert.That(result.Value!.CurrentTaxRate, Is.EqualTo(0.08m));
        Assert.That(result.Value!.HasWarnings, Is.True);
    }

    [Test]
    public async Task QuotationNotFound_ReturnsFailure()
    {
        var result = await _service.CheckConversionDiscrepanciesAsync(99999);

        Assert.That(result.IsSuccess, Is.False);
    }

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new(_options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
