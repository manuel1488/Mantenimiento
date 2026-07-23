using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Settings;
using App.Core.DTOs.Shop;
using App.Core.DTOs.Shop.Calculation;
using App.Core.Constants;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Inventory;
using App.Services.Settings;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Tests for wholesale discount resolution and sale/quotation/remission discount handling.
/// Covers the root bug: $0.01 payment validation failure when converting quotation to sale.
/// </summary>
[TestFixture]
public class WholesaleDiscountTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Region 1: ResolveWholesaleDiscount — pure unit tests, no DB needed
    // ─────────────────────────────────────────────────────────────────────

    private PricingCalculationService _pricingService = null!;

    [SetUp]
    public void Setup()
    {
        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.16m);

        var companyMock = new Mock<ICompanySettingsService>();
        companyMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto
            {
                Id = 1, CompanyName = "Test", CountryCode = "MX",
                CurrencyCode = "MXN", TimeZoneId = "America/Mexico_City"
            });

        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        _pricingService = new PricingCalculationService(
            taxRateMock.Object,
            companyMock.Object,
            roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);
    }

    #region ResolveWholesaleDiscount

    [Test]
    public void ResolveWholesaleDiscount_NoTiers_ReturnsNoDiscount()
    {
        var result = _pricingService.ResolveWholesaleDiscount([], 5m, 100m);

        Assert.That(result.HasDiscount, Is.False);
        Assert.That(result.DiscountPercentage, Is.EqualTo(0m));
        Assert.That(result.FixedDiscountAmountPerUnit, Is.Null);
    }

    [Test]
    public void ResolveWholesaleDiscount_BelowMinQuantity_ReturnsNoDiscount()
    {
        var tiers = new List<ProductWholesalePriceDto>
        {
            new() { MinQuantity = 10m, DiscountPercentage = 15m, IsActive = true }
        };

        var result = _pricingService.ResolveWholesaleDiscount(tiers, 5m, 100m);

        Assert.That(result.HasDiscount, Is.False);
    }

    [Test]
    public void ResolveWholesaleDiscount_PercentageTier_ReturnsDiscountPercentage()
    {
        var tiers = new List<ProductWholesalePriceDto>
        {
            new() { MinQuantity = 5m, DiscountPercentage = 10m, IsActive = true }
        };

        var result = _pricingService.ResolveWholesaleDiscount(tiers, 5m, 100m);

        Assert.That(result.HasDiscount, Is.True);
        Assert.That(result.IsFixedPrice, Is.False);
        Assert.That(result.DiscountPercentage, Is.EqualTo(10m));
        Assert.That(result.FixedDiscountAmountPerUnit, Is.Null);
    }

    [Test]
    public void ResolveWholesaleDiscount_FixedPriceTier_ReturnsPerUnitDiscountAmount()
    {
        // Product costs $100, wholesale fixed price is $85 → per-unit discount = $15
        var tiers = new List<ProductWholesalePriceDto>
        {
            new() { MinQuantity = 3m, FixedPrice = 85m, DiscountPercentage = 0, IsActive = true }
        };

        var result = _pricingService.ResolveWholesaleDiscount(tiers, 3m, 100m);

        Assert.That(result.HasDiscount, Is.True);
        Assert.That(result.IsFixedPrice, Is.True);
        Assert.That(result.DiscountPercentage, Is.EqualTo(0m),
            "Fixed price must NOT store a derived percentage");
        Assert.That(result.FixedDiscountAmountPerUnit, Is.EqualTo(15m));
    }

    [Test]
    public void ResolveWholesaleDiscount_FixedPriceTier_PerUnitDiscountPreservesExactAmount()
    {
        // $113.50 original, fixed price $107.00 → per-unit discount = $6.50 (exact, not derived %)
        var tiers = new List<ProductWholesalePriceDto>
        {
            new() { MinQuantity = 2m, FixedPrice = 107.00m, IsActive = true }
        };

        var result = _pricingService.ResolveWholesaleDiscount(tiers, 2m, 113.50m);

        Assert.That(result.FixedDiscountAmountPerUnit, Is.EqualTo(6.50m));
        Assert.That(result.IsFixedPrice, Is.True);
    }

    [Test]
    public void ResolveWholesaleDiscount_MultipleTiers_SelectsHighestApplicable()
    {
        // Tiers: 5+ units = 5%, 10+ units = 12%
        var tiers = new List<ProductWholesalePriceDto>
        {
            new() { MinQuantity = 5m, DiscountPercentage = 5m, IsActive = true },
            new() { MinQuantity = 10m, DiscountPercentage = 12m, IsActive = true }
        };

        var resultAt7 = _pricingService.ResolveWholesaleDiscount(tiers, 7m, 100m);
        var resultAt10 = _pricingService.ResolveWholesaleDiscount(tiers, 10m, 100m);

        Assert.That(resultAt7.DiscountPercentage, Is.EqualTo(5m), "7 units qualifies for 5% tier only");
        Assert.That(resultAt10.DiscountPercentage, Is.EqualTo(12m), "10 units qualifies for 12% tier");
    }

    #endregion

    #region WholesaleDiscountResult helper methods

    [Test]
    public void WholesaleDiscountResult_GetFixedLineDiscount_WhenFixedPrice_ReturnsPerUnitTimesQty()
    {
        var result = new WholesaleDiscountResult { FixedDiscountAmountPerUnit = 6.50m };

        Assert.That(result.GetFixedLineDiscount(3m), Is.EqualTo(19.50m));
        Assert.That(result.GetFixedLineDiscount(1m), Is.EqualTo(6.50m));
        Assert.That(result.GetFixedLineDiscount(10m), Is.EqualTo(65.00m));
    }

    [Test]
    public void WholesaleDiscountResult_GetFixedLineDiscount_WhenNoFixedPrice_ReturnsNull()
    {
        var percentageResult = new WholesaleDiscountResult { DiscountPercentage = 10m };
        var emptyResult = new WholesaleDiscountResult();

        Assert.That(percentageResult.GetFixedLineDiscount(5m), Is.Null);
        Assert.That(emptyResult.GetFixedLineDiscount(5m), Is.Null);
    }

    [Test]
    public void WholesaleDiscountResult_HasDiscount_TrueForBothTypes()
    {
        var withPct = new WholesaleDiscountResult { DiscountPercentage = 10m };
        var withFixed = new WholesaleDiscountResult { FixedDiscountAmountPerUnit = 5m };
        var empty = new WholesaleDiscountResult();

        Assert.That(withPct.HasDiscount, Is.True);
        Assert.That(withFixed.HasDiscount, Is.True);
        Assert.That(empty.HasDiscount, Is.False);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    // Region 2: SaleService integration — wholesale discount end-to-end
    // ─────────────────────────────────────────────────────────────────────

    private SaleService _saleService = null!;
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    private const int LocationId = 1;
    private const long CustomerId = 1;
    private const int PaymentMethodId = 1;
    private const string UserId = "test-user-id";

    private void SetupSaleService()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.16m);

        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        var companyMock = new Mock<ICompanySettingsService>();
        companyMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto
            {
                Id = 1, CompanyName = "Test", CountryCode = "MX",
                CurrencyCode = "MXN", TimeZoneId = "America/Mexico_City"
            });

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                Id = 1, CountryCode = "MX", BusinessName = "Test",
                TaxId = "TEST000000AA0", FiscalRegime = "601"
            });

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync(UserId);
        userMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test User");
        userMock.Setup(u => u.GetActiveLocationIdAsync()).ReturnsAsync(LocationId);

        var dateMock = new Mock<IDateTime>();
        dateMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var discountMock = new Mock<IDiscountSettingsService>();
        discountMock.Setup(d => d.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DiscountSettingsDto>.Success(new DiscountSettingsDto
            {
                Id = 1, MaximumPublicDiscount = 100, RequireAuthorizationForPublicDiscount = false
            }));

        var inventoryMock = new Mock<IContextualInventoryService>();
        inventoryMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovementOperationResult { Success = true });

        var cashMock = new Mock<ICashRegisterService>();
        cashMock.Setup(c => c.GetActiveCashRegisterAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Result<CashRegisterDto?>.Success(new CashRegisterDto
            {
                Id = 1, LocationId = LocationId, UserId = UserId,
                Status = CashRegisterStatus.Open, ExpectedCash = 0
            }));
        cashMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(Result<CashRegisterSettingsDto>.Success(
                new CashRegisterSettingsDto { IsStrictCashLimit = false }));

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns((Sale s) => new SaleDto
            {
                Id = s.Id, Subtotal = s.Subtotal, TaxAmount = s.TaxAmount,
                DiscountAmount = s.DiscountAmount, Total = s.Total
            });

        var localizerMock = new Mock<IStringLocalizer<SaleService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var pricing = new PricingCalculationService(
            taxRateMock.Object, companyMock.Object, roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);

        _saleService = new SaleService(
            new TestDbContextFactory(_dbOptions),
            mapperMock.Object,
            NullLogger<SaleService>.Instance,
            localizerMock.Object,
            userMock.Object,
            dateMock.Object,
            discountMock.Object,
            new Mock<IDiscountAuthorizerService>().Object,
            inventoryMock.Object,
            taxRateMock.Object,
            companyMock.Object,
            taxSettingsMock.Object,
            new Mock<IProductPartialSurchargeService>().Object,
            roundingMock.Object,
            cashMock.Object,
            pricing);

        SeedBaseData();
    }

    private void SeedBaseData()
    {
        using var context = new ApplicationDbContext(_dbOptions);
        context.UnitMeasures.Add(new UnitMeasure
        {
            Id = 1, Code = "PZA", Name = "Pieza", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.Customers.Add(new Customer
        {
            Id = CustomerId, Name = "Test Customer", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.PaymentMethods.Add(new PaymentMethod
        {
            Id = PaymentMethodId, Name = "Efectivo", Type = PaymentMethodType.Cash,
            IsActive = true, CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private void SeedProduct(long id, decimal price, bool isTaxable = true)
    {
        using var context = new ApplicationDbContext(_dbOptions);
        context.Products.Add(new Product
        {
            Id = id, Code = $"P{id:D4}", Name = $"Product {id}", Brand = "Test",
            Price = price, Cost = 0, IsTaxable = isTaxable, IsActive = true,
            UnitMeasureId = 1, Content = 1, QuantityStep = 1,
            RequiresInventory = false, CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private CreateSaleDto BuildSaleWithDiscount(
        long productId, decimal quantity, decimal discountPct, decimal? discountAmount,
        decimal paymentOverride = 0)
    {
        using var ctx = new ApplicationDbContext(_dbOptions);
        var product = ctx.Products.First(p => p.Id == productId);
        var gross = product.Price * quantity;
        decimal discount = discountAmount ?? Math.Round(gross * discountPct / 100, 6);
        var net = gross - discount;
        var tax = product.IsTaxable ? Math.Round(net * 0.16m, 2) : 0;
        var total = Math.Round(net, 2) + tax;

        return new CreateSaleDto
        {
            CustomerId = CustomerId,
            LocationId = LocationId,
            SaleType = SaleType.Public,
            Details =
            [
                new CreateSaleDetailDto
                {
                    ProductId = productId,
                    Quantity = quantity,
                    DiscountPercentage = discountPct,
                    DiscountAmount = discountAmount,
                    IsCustomPrice = discountAmount.HasValue
                }
            ],
            Payments =
            [
                new CreateSalePaymentDto
                {
                    PaymentMethodId = PaymentMethodId,
                    Amount = paymentOverride > 0 ? paymentOverride : total
                }
            ]
        };
    }

    [Test]
    public async Task CreateSale_WithWholesalePercentageDiscount_PaymentValidationPasses()
    {
        SetupSaleService();
        // $200 product, 10% wholesale discount, qty 5
        // Gross = $1000, discount = $100, base = $900, tax = $144, total = $1044
        SeedProduct(200, 200m);

        var dto = BuildSaleWithDiscount(200, 5m, 10m, discountAmount: null);
        var result = await _saleService.CreateSaleAsync(dto);

        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        Assert.That(result.Value!.Total, Is.EqualTo(1044.00m));
    }

    [Test]
    public async Task CreateSale_WithWholesaleFixedPriceDiscount_PaymentValidationPasses()
    {
        SetupSaleService();
        // $500 product, fixed price $470 → discount/unit = $30
        // Qty 4 → line discount = $120
        // Gross = $2000, discount = $120, base = $1880, tax = $300.80, total = $2180.80
        SeedProduct(201, 500m);

        var fixedLineDiscount = 30m * 4m; // $120 — pre-computed by ApplyWholesaleDiscount
        var dto = BuildSaleWithDiscount(201, 4m, 0m, discountAmount: fixedLineDiscount);
        var result = await _saleService.CreateSaleAsync(dto);

        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        Assert.That(result.Value!.Total, Is.EqualTo(2180.80m));
    }

    [Test]
    public async Task CreateSale_WithWholesaleFixedPriceDiscount_DiscountAmountNotRecomputedFromPercentage()
    {
        SetupSaleService();
        // This verifies that DiscountAmount in the DTO is used directly (no percentage recompute).
        // $113.50 product, fixed price $107.00 → discount/unit = $6.50
        // Qty 3 → line discount = $19.50 (exact)
        // Gross = $340.50, discount = $19.50, base = $321.00, tax = $51.36, total = $372.36
        SeedProduct(202, 113.50m);

        var lineDiscount = 6.50m * 3m; // $19.50
        var dto = BuildSaleWithDiscount(202, 3m, 0m, discountAmount: lineDiscount);
        var result = await _saleService.CreateSaleAsync(dto);

        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        Assert.That(result.Value!.Total, Is.EqualTo(372.36m));
        Assert.That(result.Value!.DiscountAmount, Is.EqualTo(19.50m));
    }

    /// <summary>
    /// Regression test for the original $0.01 payment validation bug.
    ///
    /// Scenario: COT-2026-0075 — quotation stored the exact DiscountAmount for a fixed-price
    /// wholesale rule. The old conversion code only passed DiscountPercentage (rounded to 2 dp).
    /// SaleService recomputed the discount from the rounded % → got $0.02 less → base was $0.02
    /// higher → sale total exceeded the quotation total by $0.02 → payment failed validation.
    ///
    /// Numbers: $500 product, fixed price $471.43 → discount/unit = $28.57
    ///   Exact:    base = $471.43, tax = $75.43, total = $546.86  (quotation total)
    ///   Rounded %: 5.71% → discount = $28.55, base = $471.45, total = $546.88
    ///   Drift: sale total $546.88 > payment $546.86 → validation error without fix.
    ///
    /// Fix: pass DiscountAmount = $28.57 through CreateSaleDetailDto so CalculateLine
    /// uses it directly instead of recomputing from DiscountPercentage.
    /// </summary>
    [Test]
    public async Task QuotationToSale_WithRoundedDiscountPercentage_PaymentMatchesSaleTotalExactly()
    {
        SetupSaleService();
        SeedProduct(300, 500m);

        // Exact values (from quotation with fixed-price wholesale rule)
        const decimal exactDiscount = 28.57m;   // fixed price $471.43 → discount = $500 - $471.43
        const decimal quotationTotal = 546.86m; // 471.43 base + 75.43 tax

        // WITH fix: pass DiscountAmount = $28.57 (exact), payment = quotation total
        var dtoWithFix = BuildSaleWithDiscount(300, 1m, 0m, discountAmount: exactDiscount,
            paymentOverride: quotationTotal);
        var resultWithFix = await _saleService.CreateSaleAsync(dtoWithFix);

        Assert.That(resultWithFix.IsSuccess, Is.True,
            "WITH fix: exact discount → sale total matches quotation total → payment passes");
        Assert.That(resultWithFix.Value!.Total, Is.EqualTo(quotationTotal));

        // WITHOUT fix: pass only the rounded DiscountPercentage (5.71%), payment still = quotation total
        // → sale recomputes $28.55 discount → base = $471.45 → total = $546.88 > payment $546.86
        var roundedPct = Math.Round(exactDiscount / 500m * 100m, 2); // 5.71%
        var dtoWithoutFix = BuildSaleWithDiscount(300, 1m, roundedPct, discountAmount: null,
            paymentOverride: quotationTotal);
        var resultWithoutFix = await _saleService.CreateSaleAsync(dtoWithoutFix);

        Assert.That(resultWithoutFix.IsSuccess, Is.False,
            "WITHOUT fix: rounded % → sale total > quotation total → payment validation fails");
        Assert.That(resultWithoutFix.Error, Does.Contain("less than").Or.Contain("menor"),
            "Error must mention payment is less than sale total");
    }

    [Test]
    public async Task QuotationToSale_WithPercentageDiscount_PaymentMatchesSaleTotalExactly()
    {
        SetupSaleService();
        // Standard percentage wholesale — no DiscountAmount override needed.
        // $250 product, 8% discount, qty 2
        // Gross = $500, discount = $40, base = $460, tax = $73.60, total = $533.60
        SeedProduct(301, 250m);

        var dto = BuildSaleWithDiscount(301, 2m, 8m, discountAmount: null);
        var result = await _saleService.CreateSaleAsync(dto);

        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        Assert.That(result.Value!.Total, Is.EqualTo(533.60m));
    }

    [Test]
    public async Task RemissionToSale_WithFixedPriceWholesale_DiscountAmountPreserved()
    {
        SetupSaleService();
        // Simulates consolidating a remission where DiscountAmount was stored at 6 decimal precision.
        // $337.50 product, fixed price $330 → discount/unit = $7.50
        // Qty 4 → line discount = $30.00 (exact)
        // Gross = $1350, discount = $30, base = $1320, tax = $211.20, total = $1561.20
        SeedProduct(400, 337.50m);

        var lineDiscount = 7.50m * 4m; // $30.00
        var dto = BuildSaleWithDiscount(400, 4m, 0m, discountAmount: lineDiscount);
        var result = await _saleService.CreateSaleAsync(dto);

        // Gross = $1350, discount = $30, base = $1320, tax = $211.20, total = $1531.20
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        Assert.That(result.Value!.Total, Is.EqualTo(1531.20m));
        Assert.That(result.Value!.DiscountAmount, Is.EqualTo(30.00m));
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
