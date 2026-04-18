using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Settings;
using App.Core.DTOs.Shop;
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
using Microsoft.Extensions.Logging.Abstractions;

using ShopLocation = App.Models.Shop.Location;   // avoids conflict with System.Location namespace

namespace App.Services.Tests.Shop;

/// <summary>
/// Validates inventory business rules related to quotations and sales:
///   - Creating a quotation does NOT affect inventory.
///   - Converting an accepted quotation to a sale DOES deduct inventory.
///   - Cancelling a sale that originated from a quotation reverts inventory.
///   - Products with RequiresInventory=false are never touched, even from quotation-origin sales.
/// </summary>
[TestFixture]
public class SaleInventoryTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot _dbRoot = null!;
    private Mock<IInventoryService> _inventoryMock = null!;
    private QuotationService _quotationService = null!;
    private SaleService _saleService = null!;
    private RemissionService _remissionService = null!;

    private const int LocationId = 1;
    private const int PaymentMethodId = 1;

    [SetUp]
    public void Setup()
    {
        _dbRoot = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), _dbRoot)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _inventoryMock = new Mock<IInventoryService>();

        // Moq requires explicit matchers for optional CancellationToken params (CS0854)
        _inventoryMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Successful(new InventoryMovementDto()));

        var contextFactory = new TestDbContextFactory(_dbOptions);

        var localizerQMock = new Mock<IStringLocalizer<QuotationService>>();
        localizerQMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerQMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var localizerSMock = new Mock<IStringLocalizer<SaleService>>();
        localizerSMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerSMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.UserId).Returns((string?)"test-user");
        currentUserMock.Setup(u => u.FullName).Returns((string?)"Test User");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock.Setup(t => t.GetEffectiveRateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(16m);

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto { CountryCode = "MX" });

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                CountryCode = "MX",
                BusinessName = "Test",
                TaxId = "TEST010101AAA",
                FiscalRegime = "601"
            });

        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        var discountSettingsMock = new Mock<IDiscountSettingsService>();
        discountSettingsMock.Setup(d => d.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DiscountSettingsDto>.Success(new DiscountSettingsDto
            {
                MaximumPublicDiscount = 100,
                RequireAuthorizationForPublicDiscount = false
            }));

        var cashRegisterMock = new Mock<ICashRegisterService>();
        cashRegisterMock
            .Setup(c => c.GetActiveCashRegisterAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Result<CashRegisterDto?>.Success(new CashRegisterDto { Id = 1 }));
        cashRegisterMock
            .Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(Result<CashRegisterSettingsDto>.Success(
                new CashRegisterSettingsDto { IsStrictCashLimit = false }));

        var pricingService = new PricingCalculationService(
            taxRateMock.Object,
            companySettingsMock.Object,
            roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);

        _quotationService = new QuotationService(
            contextFactory,
            new Mock<IMapper>().Object,
            NullLogger<QuotationService>.Instance,
            localizerQMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            new Mock<IEmailService>().Object,
            new Mock<IEmailTemplateService>().Object,
            new Mock<IPdfService>().Object,
            pricingService,
            new Mock<IDocumentSequenceService>().Object,
            new Mock<IQuotationSettingsService>().Object);

        _saleService = new SaleService(
            contextFactory,
            new Mock<IMapper>().Object,
            NullLogger<SaleService>.Instance,
            localizerSMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            discountSettingsMock.Object,
            new Mock<IDiscountAuthorizerService>().Object,
            _inventoryMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            taxSettingsMock.Object,
            new Mock<IProductPartialSurchargeService>().Object,
            roundingMock.Object,
            cashRegisterMock.Object,
            pricingService);

        var localizerRMock = new Mock<IStringLocalizer<RemissionService>>();
        localizerRMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerRMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var docSeqMock = new Mock<IDocumentSequenceService>();
        docSeqMock.Setup(d => d.GetNextNumberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("REM-TEST-0001");

        _remissionService = new RemissionService(
            contextFactory,
            new Mock<IMapper>().Object,
            NullLogger<RemissionService>.Instance,
            localizerRMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            _inventoryMock.Object,
            pricingService,
            new Mock<IPdfService>().Object,
            new Mock<IEmailTemplateService>().Object,
            _saleService,
            docSeqMock.Object);
    }

    // -------------------------------------------------------------------------
    // Seed helpers
    // -------------------------------------------------------------------------

    private async Task<long> SeedCustomerAsync()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var customer = new Customer
        {
            Name = "Test Customer",
            CountryCode = "MX",
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
        };
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        return customer.Id;
    }

    /// <summary>
    /// Seeds a non-taxable product so Total = UnitPrice exactly (simplifies payment setup).
    /// </summary>
    private async Task<long> SeedProductAsync(bool requiresInventory = true, decimal price = 10.00m)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);

        // UnitMeasureId is non-nullable; seed a UnitMeasure so the Include() in SaleService resolves correctly.
        var unitMeasure = new UnitMeasure
        {
            CountryCode = "MX",
            Code = "PZA",
            Name = "Pieza",
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
        };
        ctx.UnitMeasures.Add(unitMeasure);
        await ctx.SaveChangesAsync();

        var product = new Product
        {
            Name = "Test Product",
            Code = "TST-001",
            Brand = "Test Brand",
            Description = "Test Description",
            Price = price,
            IsActive = true,
            IsTaxable = false,
            RequiresInventory = requiresInventory,
            IsPartialSaleAllowed = false,
            Content = 1,
            UnitMeasureId = unitMeasure.Id,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product.Id;
    }

    private async Task SeedLocationAndPaymentMethodAsync()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);

        if (!await ctx.Locations.AnyAsync(l => l.Id == LocationId))
        {
            ctx.Locations.Add(new ShopLocation
            {
                Id = LocationId,
                Name = "Test Branch",
                Type = LocationType.Branch,
                IsActive = true,
                CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
                ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
            });
        }

        if (!await ctx.PaymentMethods.AnyAsync(p => p.Id == PaymentMethodId))
        {
            ctx.PaymentMethods.Add(new PaymentMethod
            {
                Id = PaymentMethodId,
                Name = "Cash",
                Type = PaymentMethodType.Cash,
                IsActive = true,
                CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
                ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
            });
        }

        await ctx.SaveChangesAsync();
    }

    private async Task<long> SeedAcceptedQuotationAsync(long customerId, long productId, decimal unitPrice)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = new Quotation
        {
            QuotationNumber = "COT-TEST-0001",
            CustomerId = customerId,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = QuotationStatus.Accepted,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow,
            Details =
            [
                new QuotationDetail
                {
                    ProductId = productId,
                    ProductName = "Test Product",
                    ProductCode = "TST-001",
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    TaxRate = 0,
                    CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
                }
            ]
        };
        ctx.Quotations.Add(quotation);
        await ctx.SaveChangesAsync();
        return quotation.Id;
    }

    private async Task<long> SeedSaleDirectlyAsync(long customerId, long productId, long? quotationId = null)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var sale = new Sale
        {
            CustomerId = customerId,
            LocationId = LocationId,
            SaleDate = DateTime.UtcNow,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            SaleType = SaleType.Public,
            QuotationId = quotationId,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow,
            Details =
            [
                new SaleDetail
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
                }
            ]
        };
        ctx.Sales.Add(sale);
        await ctx.SaveChangesAsync();
        return sale.Id;
    }

    private CreateSaleDto BuildSaleDto(long customerId, long productId, long? quotationId, decimal total)
    {
        return new CreateSaleDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            SaleDate = DateTime.UtcNow,
            SaleType = SaleType.Public,
            QuotationId = quotationId,
            Details =
            [
                new CreateSaleDetailDto
                {
                    ProductId = productId,
                    Quantity = 1,
                    DiscountPercentage = 0,
                    UnitPrice = total,
                    IsCustomPrice = true
                }
            ],
            Payments =
            [
                new CreateSalePaymentDto
                {
                    PaymentMethodId = PaymentMethodId,
                    Amount = total
                }
            ]
        };
    }

    // =========================================================================
    // Rule 1: Quotation creation does NOT affect inventory
    // =========================================================================

    [Test]
    public async Task CreatingQuotation_DoesNotCreateInventoryMovements()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);

        await _quotationService.CreateAsync(new CreateQuotationDto
        {
            CustomerId = customerId,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Details =
            [
                new CreateQuotationDetailDto
                {
                    ProductId = productId,
                    Quantity = 5,
                    UnitPrice = 10.00m,
                    DiscountPercentage = 0
                }
            ]
        });

        await using var ctx = new ApplicationDbContext(_dbOptions);
        Assert.That(await ctx.InventoryMovements.CountAsync(), Is.EqualTo(0),
            "Creating a quotation must not generate any inventory movements.");

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "IInventoryService.CreateMovementAsync must never be called during quotation creation.");
    }

    // =========================================================================
    // Rule 2: Converting quotation to sale DEDUCTS inventory
    // =========================================================================

    [Test]
    public async Task ConvertingQuotationToSale_CreatesInventoryDeduction()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true, price: 10.00m);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);

        var result = await _saleService.CreateSaleAsync(
            BuildSaleDto(customerId, productId, quotationId, total: 10.00m));

        Assert.That(result.IsSuccess, Is.True, result.Error);

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.Is<CreateInventoryMovementDto>(d =>
                    d.MovementType == InventoryMovementType.Sale &&
                    d.ProductId == productId &&
                    d.Quantity == 1),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Converting a quotation to a sale must deduct inventory with MovementType=Sale.");
    }

    [Test]
    public async Task ConvertingQuotationToSale_QuotationStatusBecomesConvertedToSale()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true, price: 10.00m);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);

        await _saleService.CreateSaleAsync(
            BuildSaleDto(customerId, productId, quotationId, total: 10.00m));

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = await ctx.Quotations.FindAsync(quotationId);
        Assert.That(quotation!.Status, Is.EqualTo(QuotationStatus.ConvertedToSale),
            "After conversion the quotation status must be ConvertedToSale.");
    }

    // =========================================================================
    // Rule 3: Cancelling a sale from quotation REVERTS inventory
    // =========================================================================

    [Test]
    public async Task CancellingSaleFromQuotation_CreatesInventoryReturn()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);
        var saleId = await SeedSaleDirectlyAsync(customerId, productId, quotationId);

        var result = await _saleService.CancelSaleAsync(saleId, "Test cancellation");

        Assert.That(result.IsSuccess, Is.True, result.Error);

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.Is<CreateInventoryMovementDto>(d =>
                    d.MovementType == InventoryMovementType.Return &&
                    d.ProductId == productId &&
                    d.Quantity == 1),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Cancelling a sale from a quotation must return inventory with MovementType=Return.");
    }

    [Test]
    public async Task CancellingSaleFromQuotation_SaleStatusBecomesCancelled()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);
        var saleId = await SeedSaleDirectlyAsync(customerId, productId, quotationId);

        await _saleService.CancelSaleAsync(saleId, "Test cancellation");

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var sale = await ctx.Sales.FindAsync(saleId);
        Assert.That(sale!.Status, Is.EqualTo(App.Core.Enums.Shop.SaleStatus.Cancelled));
    }

    // =========================================================================
    // Rule 4: Products that don't require inventory tracking are never deducted
    // =========================================================================

    [Test]
    public async Task ConvertingQuotationToSale_ProductNotRequiringInventory_NoMovementCreated()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: false, price: 10.00m);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);

        var result = await _saleService.CreateSaleAsync(
            BuildSaleDto(customerId, productId, quotationId, total: 10.00m));

        Assert.That(result.IsSuccess, Is.True, result.Error);

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Products with RequiresInventory=false must never generate inventory movements.");
    }

    [Test]
    public async Task CancellingSale_ProductNotRequiringInventory_NoReturnMovementCreated()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: false);
        var saleId = await SeedSaleDirectlyAsync(customerId, productId, quotationId: null);

        await _saleService.CancelSaleAsync(saleId, "Test");

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Cancelling a sale with RequiresInventory=false product must not create a return movement.");
    }

    // =========================================================================
    // Rule 5: Converting quotation to remission deducts inventory + marks quotation
    // =========================================================================

    [Test]
    public async Task ConvertingQuotationToRemission_CreatesInventoryDeduction()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true, price: 10.00m);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);

        var result = await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details =
            [
                new CreateRemissionDetailDto
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    DiscountPercentage = 0
                }
            ]
        });

        Assert.That(result.IsSuccess, Is.True, result.Error);

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.Is<CreateInventoryMovementDto>(d =>
                    d.MovementType == InventoryMovementType.Sale &&
                    d.ProductId == productId &&
                    d.Quantity == 1),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Converting a quotation to a remission must deduct inventory with MovementType=Sale.");
    }

    [Test]
    public async Task ConvertingQuotationToRemission_InventoryMovementSubTypeIsRemission()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true, price: 10.00m);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);

        var result = await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details =
            [
                new CreateRemissionDetailDto
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    DiscountPercentage = 0
                }
            ]
        });

        Assert.That(result.IsSuccess, Is.True, result.Error);

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.Is<CreateInventoryMovementDto>(d =>
                    d.MovementType == InventoryMovementType.Sale &&
                    d.MovementSubType == InventoryMovementSubType.Remission &&
                    d.ProductId == productId),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Remission inventory movement must use MovementSubType=Remission, not DirectSale.");
    }

    [Test]
    public async Task ConvertingQuotationToRemission_QuotationStatusBecomesConvertedToRemission()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true, price: 10.00m);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId, unitPrice: 10.00m);

        await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details =
            [
                new CreateRemissionDetailDto
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    DiscountPercentage = 0
                }
            ]
        });

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = await ctx.Quotations.FindAsync(quotationId);
        Assert.That(quotation!.Status, Is.EqualTo(QuotationStatus.ConvertedToRemission),
            "After conversion the quotation status must be ConvertedToRemission.");
    }

    // =========================================================================
    // Infrastructure
    // =========================================================================

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new(_options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken _ = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
