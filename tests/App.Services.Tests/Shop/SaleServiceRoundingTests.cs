using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Settings;
using App.Core.DTOs.Shop;
using App.Core.Constants;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Settings;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Tests that all monetary values in sale calculations are rounded to 2 decimal places,
/// preventing payment validation failures when product prices have up to 6 decimals.
/// </summary>
[TestFixture]
public class SaleServiceRoundingTests
{
    private SaleService _saleService = null!;
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<IMapper> _mapperMock = null!;
    private Mock<ITaxRateService> _taxRateServiceMock = null!;
    private Mock<IRoundingSettingsService> _roundingSettingsServiceMock = null!;
    private Mock<ICompanySettingsService> _companySettingsServiceMock = null!;
    private Mock<ITaxSettingsService> _taxSettingsServiceMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IDateTime> _dateTimeMock = null!;
    private Mock<IDiscountSettingsService> _discountSettingsServiceMock = null!;
    private Mock<IDiscountAuthorizerService> _discountAuthorizerServiceMock = null!;
    private Mock<IInventoryService> _inventoryServiceMock = null!;
    private Mock<IProductPartialSurchargeService> _partialSurchargeServiceMock = null!;
    private Mock<ICashRegisterService> _cashRegisterServiceMock = null!;

    private const int LocationId = 1;
    private const long CustomerId = 1;
    private const int PaymentMethodId = 1;
    private const string UserId = "test-user-id";

    [SetUp]
    public void Setup()
    {
        // InMemory database — unique per test
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Mapper — returns a SaleDto echoing the Sale entity values
        _mapperMock = new Mock<IMapper>();
        _mapperMock.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns((Sale s) => new SaleDto
            {
                Id = s.Id,
                Subtotal = s.Subtotal,
                TaxAmount = s.TaxAmount,
                DiscountAmount = s.DiscountAmount,
                RoundingAmount = s.RoundingAmount,
                Total = s.Total,
                SaleType = s.SaleType,
                LocationId = s.LocationId
            });

        // Tax rate: 16% IVA
        _taxRateServiceMock = new Mock<ITaxRateService>();
        _taxRateServiceMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.16m);

        // Rounding: disabled (passthrough)
        _roundingSettingsServiceMock = new Mock<IRoundingSettingsService>();
        _roundingSettingsServiceMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        // Company settings
        _companySettingsServiceMock = new Mock<ICompanySettingsService>();
        _companySettingsServiceMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto
            {
                Id = 1,
                CompanyName = "Test Company",
                CountryCode = "MX",
                CurrencyCode = "MXN",
                TimeZoneId = "America/Mexico_City"
            });

        // Tax settings
        _taxSettingsServiceMock = new Mock<ITaxSettingsService>();
        _taxSettingsServiceMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                Id = 1,
                CountryCode = "MX",
                BusinessName = "Test",
                TaxId = "TEST000000AA0",
                FiscalRegime = "601"
            });

        // Current user
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(u => u.UserId).Returns(UserId);
        _currentUserServiceMock.Setup(u => u.FullName).Returns("Test User");
        _currentUserServiceMock.Setup(u => u.ActiveLocationId).Returns(LocationId);

        // DateTime
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        // Discount settings: no restrictions
        _discountSettingsServiceMock = new Mock<IDiscountSettingsService>();
        _discountSettingsServiceMock.Setup(d => d.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DiscountSettingsDto>.Success(new DiscountSettingsDto
            {
                Id = 1,
                MaximumPublicDiscount = 100,
                RequireAuthorizationForPublicDiscount = false
            }));

        // Discount authorizer: not needed (no discount)
        _discountAuthorizerServiceMock = new Mock<IDiscountAuthorizerService>();

        // Inventory: always available
        _inventoryServiceMock = new Mock<IInventoryService>();
        _inventoryServiceMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _inventoryServiceMock
            .Setup(i => i.CreateMovementAsync(It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovementOperationResult { Success = true });

        // Partial surcharge: default (not used unless overridden)
        _partialSurchargeServiceMock = new Mock<IProductPartialSurchargeService>();

        // Cash register: active
        _cashRegisterServiceMock = new Mock<ICashRegisterService>();
        _cashRegisterServiceMock
            .Setup(c => c.GetActiveCashRegisterAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Result<CashRegisterDto?>.Success(new CashRegisterDto
            {
                Id = 1,
                LocationId = LocationId,
                UserId = UserId,
                Status = CashRegisterStatus.Open,
                ExpectedCash = 0
            }));
        _cashRegisterServiceMock
            .Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(Result<CashRegisterSettingsDto>.Success(new CashRegisterSettingsDto
            {
                IsStrictCashLimit = false
            }));

        // Build SaleService
        var contextFactory = new TestDbContextFactory(_dbOptions);
        var localizerMock = new Mock<IStringLocalizer<SaleService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) =>
                new LocalizedString(key, string.Format(key, args)));

        _saleService = new SaleService(
            contextFactory,
            _mapperMock.Object,
            NullLogger<SaleService>.Instance,
            localizerMock.Object,
            _currentUserServiceMock.Object,
            _dateTimeMock.Object,
            _discountSettingsServiceMock.Object,
            _discountAuthorizerServiceMock.Object,
            _inventoryServiceMock.Object,
            _taxRateServiceMock.Object,
            _companySettingsServiceMock.Object,
            _taxSettingsServiceMock.Object,
            _partialSurchargeServiceMock.Object,
            _roundingSettingsServiceMock.Object,
            _cashRegisterServiceMock.Object,
            new PricingCalculationService(
                _taxRateServiceMock.Object,
                _companySettingsServiceMock.Object,
                _roundingSettingsServiceMock.Object,
                NullLogger<PricingCalculationService>.Instance));

        // Seed base data
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var context = new ApplicationDbContext(_dbOptions);

        var unitMeasure = new UnitMeasure
        {
            Id = 1,
            Code = "PZA",
            Name = "Pieza",
            CountryCode = "MX",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };
        context.UnitMeasures.Add(unitMeasure);

        context.Customers.Add(new Customer
        {
            Id = CustomerId,
            Name = "Test Customer",
            CountryCode = "MX",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        context.PaymentMethods.Add(new PaymentMethod
        {
            Id = PaymentMethodId,
            Name = "Efectivo",
            Type = PaymentMethodType.Cash,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
    }

    private void SeedProduct(long id, decimal price, bool isTaxable = true, bool requiresInventory = false,
        bool isPartialSaleAllowed = false, decimal content = 1)
    {
        using var context = new ApplicationDbContext(_dbOptions);
        context.Products.Add(new Product
        {
            Id = id,
            Code = $"P{id:D4}",
            Name = $"Product {id}",
            Brand = "Test",
            Price = price,
            Cost = 0,
            IsTaxable = isTaxable,
            IsActive = true,
            UnitMeasureId = 1,
            Content = content,
            IsPartialSaleAllowed = isPartialSaleAllowed,
            QuantityStep = 1,
            RequiresInventory = requiresInventory,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private CreateSaleDto BuildSaleDto(params (long ProductId, decimal Quantity, decimal DiscountPct)[] items)
    {
        decimal paymentTotal = 0;
        foreach (var item in items)
        {
            using var ctx = new ApplicationDbContext(_dbOptions);
            var product = ctx.Products.First(p => p.Id == item.ProductId);
            var subtotal = Math.Round(product.Price * item.Quantity, 2);
            var discount = Math.Round(subtotal * (item.DiscountPct / 100), 2);
            var afterDiscount = subtotal - discount;
            var tax = product.IsTaxable ? Math.Round(afterDiscount * 0.16m, 2) : 0;
            paymentTotal += afterDiscount + tax;
        }

        return new CreateSaleDto
        {
            CustomerId = CustomerId,
            LocationId = LocationId,
            SaleType = SaleType.Public,
            DiscountPercentage = 0,
            Details = items.Select(i => new CreateSaleDetailDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                DiscountPercentage = i.DiscountPct
            }).ToList(),
            Payments = new List<CreateSalePaymentDto>
            {
                new() { PaymentMethodId = PaymentMethodId, Amount = Math.Round(paymentTotal, 2) }
            }
        };
    }

    private static bool HasAtMost2Decimals(decimal value)
        => value == Math.Round(value, 2);

    // ─── Test Cases ───

    [Test]
    public async Task CreateSale_PriceWith6Decimals_AllAmountsHave2Decimals()
    {
        // Arrange: price with 6 decimal places
        SeedProduct(100, 45.123456m);
        var dto = BuildSaleDto((100, 2m, 0));

        // Act
        var result = await _saleService.CreateSaleAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        var sale = result.Value!;
        Assert.That(HasAtMost2Decimals(sale.Subtotal), Is.True, $"Subtotal has >2 decimals: {sale.Subtotal}");
        Assert.That(HasAtMost2Decimals(sale.TaxAmount), Is.True, $"TaxAmount has >2 decimals: {sale.TaxAmount}");
        Assert.That(HasAtMost2Decimals(sale.DiscountAmount), Is.True, $"DiscountAmount has >2 decimals: {sale.DiscountAmount}");
        Assert.That(HasAtMost2Decimals(sale.Total), Is.True, $"Total has >2 decimals: {sale.Total}");
    }

    [Test]
    public async Task CreateSale_WholePrice_AllAmountsHave2Decimals()
    {
        // Arrange: clean price, no rounding issues
        SeedProduct(101, 45.00m);
        var dto = BuildSaleDto((101, 2m, 0));

        // Act
        var result = await _saleService.CreateSaleAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        var sale = result.Value!;
        Assert.That(sale.Total, Is.EqualTo(104.40m)); // 90 + 14.40 IVA
        Assert.That(HasAtMost2Decimals(sale.Total), Is.True);
    }

    [Test]
    public async Task CreateSale_RepeatingDecimalPriceWithTax_TotalHas2Decimals()
    {
        // Arrange: 33.333333 * 3 = 99.999999 → should round to 100.00
        SeedProduct(102, 33.333333m, isTaxable: true);
        var dto = BuildSaleDto((102, 3m, 0));

        // Act
        var result = await _saleService.CreateSaleAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        var sale = result.Value!;
        Assert.That(HasAtMost2Decimals(sale.Subtotal), Is.True, $"Subtotal: {sale.Subtotal}");
        Assert.That(HasAtMost2Decimals(sale.TaxAmount), Is.True, $"TaxAmount: {sale.TaxAmount}");
        Assert.That(HasAtMost2Decimals(sale.Total), Is.True, $"Total: {sale.Total}");
        Assert.That(sale.Subtotal, Is.EqualTo(100.00m));
    }

    [Test]
    public async Task CreateSale_PriceWithDecimalsAndLineDiscount_AllAmountsHave2Decimals()
    {
        // Arrange: 99.999999 with 10% line discount
        SeedProduct(103, 99.999999m, isTaxable: true);
        var dto = BuildSaleDto((103, 1m, 10m));

        // Act
        var result = await _saleService.CreateSaleAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        var sale = result.Value!;
        Assert.That(HasAtMost2Decimals(sale.Subtotal), Is.True, $"Subtotal: {sale.Subtotal}");
        Assert.That(HasAtMost2Decimals(sale.DiscountAmount), Is.True, $"DiscountAmount: {sale.DiscountAmount}");
        Assert.That(HasAtMost2Decimals(sale.TaxAmount), Is.True, $"TaxAmount: {sale.TaxAmount}");
        Assert.That(HasAtMost2Decimals(sale.Total), Is.True, $"Total: {sale.Total}");
    }

    [Test]
    public async Task CreateSale_MultipleProductsWithDecimalPrices_TotalHas2Decimals()
    {
        // Arrange: 3 products with varied 6-decimal prices
        SeedProduct(104, 12.345678m, isTaxable: true);
        SeedProduct(105, 0.500001m, isTaxable: false);
        SeedProduct(106, 999.999999m, isTaxable: true);

        var dto = BuildSaleDto(
            (104, 3m, 0),
            (105, 10m, 0),
            (106, 1m, 0));

        // Act
        var result = await _saleService.CreateSaleAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        var sale = result.Value!;
        Assert.That(HasAtMost2Decimals(sale.Subtotal), Is.True, $"Subtotal: {sale.Subtotal}");
        Assert.That(HasAtMost2Decimals(sale.TaxAmount), Is.True, $"TaxAmount: {sale.TaxAmount}");
        Assert.That(HasAtMost2Decimals(sale.Total), Is.True, $"Total: {sale.Total}");
    }

    [Test]
    public async Task CreateSale_PartialSaleWithSurcharge_AllAmountsHave2Decimals()
    {
        // Arrange: partial sale product with surcharge returning many decimals
        SeedProduct(107, 100.00m, isTaxable: true, isPartialSaleAllowed: true, content: 4);

        _partialSurchargeServiceMock
            .Setup(p => p.CalculateFractionalPriceAsync(107, 1m, 4m, 100.00m, null))
            .ReturnsAsync(Result<FractionalPriceCalculationDto>.Success(new FractionalPriceCalculationDto
            {
                ProductId = 107,
                BaseUnitPrice = 25.00m,
                Quantity = 1m,
                FractionId = null,
                SurchargePercentage = 33.333333m,
                BasePriceBeforeSurcharge = 25.00m,
                SurchargeAmount = 8.333333m, // many decimals
                FinalPrice = 33.333333m       // many decimals
            }));

        var paymentAmount = Math.Round(33.33m * 1.16m, 2); // approximate
        var dto = new CreateSaleDto
        {
            CustomerId = CustomerId,
            LocationId = LocationId,
            SaleType = SaleType.Public,
            DiscountPercentage = 0,
            Details = new List<CreateSaleDetailDto>
            {
                new() { ProductId = 107, Quantity = 1m, DiscountPercentage = 0 }
            },
            Payments = new List<CreateSalePaymentDto>
            {
                new() { PaymentMethodId = PaymentMethodId, Amount = 100.00m } // overpay to ensure it covers
            }
        };

        // Act
        var result = await _saleService.CreateSaleAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Sale failed: {result.Error}");
        var sale = result.Value!;
        Assert.That(HasAtMost2Decimals(sale.Subtotal), Is.True, $"Subtotal: {sale.Subtotal}");
        Assert.That(HasAtMost2Decimals(sale.TaxAmount), Is.True, $"TaxAmount: {sale.TaxAmount}");
        Assert.That(HasAtMost2Decimals(sale.Total), Is.True, $"Total: {sale.Total}");
    }

    [Test]
    public async Task CreateSale_PaymentExactlyMatchesTotal_SaleSucceeds()
    {
        // This is the exact bug scenario: price * qty gives a clean number
        // but with 6-decimal prices the internal calculation could drift.
        SeedProduct(108, 45.000000m, isTaxable: true);
        var dto = BuildSaleDto((108, 2m, 0));

        // Payment = 90 + 14.40 IVA = 104.40
        var result = await _saleService.CreateSaleAsync(dto);

        Assert.That(result.IsSuccess, Is.True, $"Sale should succeed but got: {result.Error}");
        Assert.That(result.Value!.Total, Is.EqualTo(104.40m));
    }

    /// <summary>
    /// IDbContextFactory implementation for tests using InMemory database.
    /// </summary>
    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext()
            => new(_options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
