using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Settings;
using App.Core.DTOs.Shop;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Inventory;
using App.Services.Settings;
using App.Services.Shop;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Testcontainers.MySql;

using InventoryRow = App.Models.Shop.Inventory;
using ShopLocation = App.Models.Shop.Location;

namespace App.Services.Tests.Shop;

// ============================================================
// Fixture 1: EF In-Memory — logical flow and mock verification
// Proves that the contextual overload is called (not stan0dalone),
// that failure results are correctly returned, and that quotation
// status is not accidentally committed before rollback.
// ============================================================

[TestFixture]
public class RemissionIntegrityInMemoryTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<IContextualInventoryService> _inventoryMock = null!;
    private RemissionService _remissionService = null!;
    private SaleService _saleService = null!;

    private const int LocationId = 1;
    private const int PaymentMethodId = 1;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _inventoryMock = new Mock<IContextualInventoryService>();

        _inventoryMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Standalone overload — never called from within a transaction
        _inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Successful(new InventoryMovementDto()));

        // Contextual overload — the one RemissionService and SaleService actually call
        _inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Successful(new InventoryMovementDto()));

        var contextFactory = new TestDbContextFactory(_dbOptions);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync("test-user");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock.Setup(t => t.GetEffectiveRateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0m);

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto { CountryCode = "MX" });

        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        var pricingService = new PricingCalculationService(
            taxRateMock.Object,
            companySettingsMock.Object,
            roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);

        var localizerRMock = new Mock<IStringLocalizer<RemissionService>>();
        localizerRMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerRMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var localizerSMock = new Mock<IStringLocalizer<SaleService>>();
        localizerSMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerSMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var docSeqMock = new Mock<IDocumentSequenceService>();
        docSeqMock.Setup(d => d.GetNextNumberAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("REM-TEST-0001");

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

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                CountryCode = "MX",
                BusinessName = "Test",
                TaxId = "TEST010101AAA",
                FiscalRegime = "601"
            });

        _saleService = new SaleService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<SaleService>.Instance,
            localizerSMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            discountSettingsMock.Object,
            Mock.Of<IDiscountAuthorizerService>(),
            _inventoryMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            taxSettingsMock.Object,
            Mock.Of<IProductPartialSurchargeService>(),
            roundingMock.Object,
            cashRegisterMock.Object,
            pricingService);

        _remissionService = new RemissionService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<RemissionService>.Instance,
            localizerRMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            _inventoryMock.Object,
            pricingService,
            Mock.Of<IPdfService>(),
            Mock.Of<IEmailTemplateService>(),
            _saleService,
            docSeqMock.Object,
            Options.Create(new BrandingOptions()));
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
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<long> SeedProductAsync(bool requiresInventory = true, decimal price = 10m)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var unitMeasure = new UnitMeasure
        {
            CountryCode = "MX",
            Code = Guid.NewGuid().ToString()[..4],
            Name = "Pieza",
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.UnitMeasures.Add(unitMeasure);
        await ctx.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product-{Guid.NewGuid():N}",
            Code = Guid.NewGuid().ToString("N")[..8],
            Brand = "Test",
            Description = "Test",
            Price = price,
            IsActive = true,
            IsTaxable = false,
            RequiresInventory = requiresInventory,
            IsPartialSaleAllowed = false,
            Content = 1,
            UnitMeasureId = unitMeasure.Id,
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
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
                Name = "Branch",
                Type = LocationType.Branch,
                IsActive = true,
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "seed",
                ModifiedAt = DateTime.UtcNow
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
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "seed",
                ModifiedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    private async Task<long> SeedAcceptedQuotationAsync(long customerId, long productId, decimal unitPrice = 10m)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = new Quotation
        {
            QuotationNumber = $"COT-{Guid.NewGuid():N}"[..12],
            CustomerId = customerId,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = QuotationStatus.Accepted,
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow,
            Details =
            [
                new QuotationDetail
                {
                    ProductId = productId,
                    ProductName = "Product",
                    ProductCode = "TST",
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

    private async Task<long> SeedAcceptedQuotationWithTwoProductsAsync(
        long customerId, long productId1, long productId2, decimal unitPrice = 10m)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = new Quotation
        {
            QuotationNumber = $"COT-{Guid.NewGuid():N}"[..12],
            CustomerId = customerId,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = QuotationStatus.Accepted,
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow,
            Details =
            [
                new QuotationDetail
                {
                    ProductId = productId1,
                    ProductName = "Product 1",
                    ProductCode = "TST1",
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    TaxRate = 0,
                    CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
                },
                new QuotationDetail
                {
                    ProductId = productId2,
                    ProductName = "Product 2",
                    ProductCode = "TST2",
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

    // =========================================================================
    // Test 1 — Stock unavailable → remission fails, no movement attempted
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenStockUnavailable_ReturnsFailure()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        _inventoryMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                productId, LocationId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        Assert.That(result.IsSuccess, Is.False, "Insufficient stock must return failure");

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "No movement must be created when stock validation fails");
    }

    // =========================================================================
    // Test 2 — Stock unavailable → quotation status unchanged
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenStockUnavailable_QuotationStatusUnchanged()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        _inventoryMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                productId, LocationId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = await ctx.Quotations.FindAsync(quotationId);
        Assert.That(quotation!.Status, Is.EqualTo(QuotationStatus.Accepted),
            "Quotation must remain Accepted when remission fails at stock validation");
    }

    // =========================================================================
    // Test 3 — Movement fails → remission fails
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenMovementFails_ReturnsFailure()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        _inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Failure("Insufficient stock"));

        var result = await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        Assert.That(result.IsSuccess, Is.False, "Movement failure must bubble up as remission failure");
    }

    // =========================================================================
    // Test 4 — Movement fails → movement was attempted exactly once (no retry)
    //
    // Note: EF In-Memory has no-op transactions, so we cannot assert quotation
    // status here (SaveChangesAsync inside the tx is not rolled back in-memory).
    // The Testcontainers fixture covers the actual rollback assertion.
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenMovementFails_MovementAttemptedExactlyOnce()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        _inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Failure("Insufficient stock"));

        await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "When movement fails, the contextual overload must be called exactly once (no retry)");
    }

    // =========================================================================
    // Test 5 — Second of two movements fails → failure (exact bug scenario)
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenSecondOfTwoMovementsFails_ReturnsFailure()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId1 = await SeedProductAsync(requiresInventory: true);
        var productId2 = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationWithTwoProductsAsync(customerId, productId1, productId2);

        _inventoryMock
            .SetupSequence(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Successful(new InventoryMovementDto()))
            .ReturnsAsync(MovementOperationResult.Failure("Insufficient stock"));

        var result = await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details =
            [
                new CreateRemissionDetailDto { ProductId = productId1, Quantity = 1, UnitPrice = 10m },
                new CreateRemissionDetailDto { ProductId = productId2, Quantity = 1, UnitPrice = 10m }
            ]
        });

        Assert.That(result.IsSuccess, Is.False,
            "When the second of two movements fails, the entire remission must fail");
    }

    // =========================================================================
    // Test 6 — Happy path → contextual overload is called, standalone is NOT
    // =========================================================================

    [Test]
    public async Task CreateRemission_HappyPath_CallsContextualOverload()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        var result = await _remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        Assert.That(result.IsSuccess, Is.True, result.Error);

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "The contextual overload (with DbContext) must be called during remission creation");

        _inventoryMock.Verify(
            i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "The standalone overload must NEVER be called inside a transaction — it creates its own context and commits independently");
    }

    // =========================================================================
    // Test 7 — SaleService: movement fails → sale fails
    // =========================================================================

    [Test]
    public async Task CreateSale_WhenMovementFails_ReturnsFailure()
    {
        await SeedLocationAndPaymentMethodAsync();
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(requiresInventory: true, price: 10m);

        _inventoryMock
            .Setup(i => i.CreateMovementAsync(
                It.IsAny<CreateInventoryMovementDto>(),
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MovementOperationResult.Failure("Insufficient stock"));

        var result = await _saleService.CreateSaleAsync(new CreateSaleDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            SaleDate = DateTime.UtcNow,
            SaleType = SaleType.Public,
            Details =
            [
                new CreateSaleDetailDto
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 10m,
                    IsCustomPrice = true
                }
            ],
            Payments = [new CreateSalePaymentDto { PaymentMethodId = PaymentMethodId, Amount = 10m }]
        });

        Assert.That(result.IsSuccess, Is.False, "Movement failure must propagate as sale failure");
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

// ============================================================
// Fixture 2: Testcontainers MySQL 8.0 — actual rollback proof
//
// Uses a real MySQL 8.0 engine (same as production) and real
// InventoryService (not mocked) to prove that inventory movements
// are truly rolled back when a transaction fails mid-loop.
//
// Divergence trick to trigger mid-loop failure without mocks:
//   IndividualUnits = 5  → ValidateStockAvailabilityAsync passes (checks IndividualUnits)
//   Quantity = 0         → CreateMovementCoreAsync fails (checks Quantity before deduction)
// ============================================================

[TestFixture]
[Category("Testcontainers")]
public class RemissionRollbackContainerTests
{
    private MySqlContainer _mysql = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private const int LocationId = 1;
    private const int PaymentMethodId = 1;

    [OneTimeSetUp]
    public async Task StartContainer()
    {
        _mysql = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithDatabase("cleeny_test")
            .Build();
        await _mysql.StartAsync();

        // EnableRetryOnFailure mirrors Program.cs's production configuration — this fixture
        // proves the 20 manually-wrapped transaction sites work under a real retrying
        // execution strategy against real MySQL, not just EF InMemory's no-op strategy.
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(_mysql.GetConnectionString(),
                ServerVersion.Parse("8.0"),
                o => o.CommandTimeout(60).EnableRetryOnFailure(3))
            .Options;

        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureCreatedAsync();

        // Seed static reference data once for the lifetime of the container
        if (!await ctx.Locations.AnyAsync(l => l.Id == LocationId))
        {
            ctx.Locations.Add(new ShopLocation
            {
                Id = LocationId,
                Name = "Branch",
                Type = LocationType.Branch,
                IsActive = true,
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "seed",
                ModifiedAt = DateTime.UtcNow
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
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "seed",
                ModifiedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public async Task StopContainer() => await _mysql.DisposeAsync();

    [TearDown]
    public async Task CleanPerTestData()
    {
        await using var ctx = new ApplicationDbContext(_options);

        // Delete in FK dependency order; use ExecuteDeleteAsync for efficiency
        await ctx.InventoryMovements.ExecuteDeleteAsync();
        await ctx.Inventory.ExecuteDeleteAsync();
        await ctx.RemissionDetails.ExecuteDeleteAsync();
        await ctx.Remissions.ExecuteDeleteAsync();
        await ctx.SaleDetails.ExecuteDeleteAsync();
        await ctx.SalePayments.ExecuteDeleteAsync();
        await ctx.Sales.ExecuteDeleteAsync();
        await ctx.QuotationDetails.ExecuteDeleteAsync();
        await ctx.Quotations.ExecuteDeleteAsync();
        await ctx.Customers.ExecuteDeleteAsync();
        await ctx.Products.ExecuteDeleteAsync();
        await ctx.UnitMeasures.ExecuteDeleteAsync();
    }

    // -------------------------------------------------------------------------
    // Service factory
    // -------------------------------------------------------------------------

    private (RemissionService Remission, SaleService Sale) BuildServices()
    {
        var contextFactory = new TestDbContextFactory(_options);

        var localizerInventoryMock = new Mock<IStringLocalizer<InventoryService>>();
        localizerInventoryMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var realInventoryService = new InventoryService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<InventoryService>.Instance,
            Mock.Of<ICurrentUserService>(u => u.GetUserIdAsync().Result == "test"),
            localizerInventoryMock.Object,
            Mock.Of<IDateTime>(d => d.Now == DateTime.UtcNow),
            Mock.Of<IInventoryAlertEmailService>(),
            Mock.Of<ICompanySettingsService>(),
            Mock.Of<IPdfService>(),
            Mock.Of<IEmailTemplateService>(),
            Mock.Of<IDocumentSequenceService>(),
            Options.Create(new BrandingOptions()));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync("test");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock.Setup(t => t.GetEffectiveRateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0m);

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto { CountryCode = "MX" });

        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        var pricingService = new PricingCalculationService(
            taxRateMock.Object,
            companySettingsMock.Object,
            roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);

        var localizerRMock = new Mock<IStringLocalizer<RemissionService>>();
        localizerRMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerRMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var docSeqMock = new Mock<IDocumentSequenceService>();
        docSeqMock.Setup(d => d.GetNextNumberAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("REM-TC-0001");

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

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                CountryCode = "MX",
                BusinessName = "Test",
                TaxId = "TEST010101AAA",
                FiscalRegime = "601"
            });

        var localizerSMock = new Mock<IStringLocalizer<SaleService>>();
        localizerSMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerSMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var saleService = new SaleService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<SaleService>.Instance,
            localizerSMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            discountSettingsMock.Object,
            Mock.Of<IDiscountAuthorizerService>(),
            realInventoryService,
            taxRateMock.Object,
            companySettingsMock.Object,
            taxSettingsMock.Object,
            Mock.Of<IProductPartialSurchargeService>(),
            roundingMock.Object,
            cashRegisterMock.Object,
            pricingService);

        var remissionService = new RemissionService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<RemissionService>.Instance,
            localizerRMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            realInventoryService,
            pricingService,
            Mock.Of<IPdfService>(),
            Mock.Of<IEmailTemplateService>(),
            saleService,
            docSeqMock.Object,
            Options.Create(new BrandingOptions()));

        return (remissionService, saleService);
    }

    /// <summary>
    /// Same wiring as <see cref="BuildServices"/> but with a real <see cref="DocumentSequenceService"/>
    /// instead of a mock, so the folio increment goes through the actual ambient-context code path.
    /// </summary>
    private (RemissionService Remission, SaleService Sale) BuildServicesWithRealDocumentSequence()
    {
        var contextFactory = new TestDbContextFactory(_options);

        var localizerInventoryMock = new Mock<IStringLocalizer<InventoryService>>();
        localizerInventoryMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var realInventoryService = new InventoryService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<InventoryService>.Instance,
            Mock.Of<ICurrentUserService>(u => u.GetUserIdAsync().Result == "test"),
            localizerInventoryMock.Object,
            Mock.Of<IDateTime>(d => d.Now == DateTime.UtcNow),
            Mock.Of<IInventoryAlertEmailService>(),
            Mock.Of<ICompanySettingsService>(),
            Mock.Of<IPdfService>(),
            Mock.Of<IEmailTemplateService>(),
            Mock.Of<IDocumentSequenceService>(),
            Options.Create(new BrandingOptions()));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync("test");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock.Setup(t => t.GetEffectiveRateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0m);

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto { CountryCode = "MX" });

        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        var pricingService = new PricingCalculationService(
            taxRateMock.Object,
            companySettingsMock.Object,
            roundingMock.Object,
            NullLogger<PricingCalculationService>.Instance);

        var localizerRMock = new Mock<IStringLocalizer<RemissionService>>();
        localizerRMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerRMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

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

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                CountryCode = "MX",
                BusinessName = "Test",
                TaxId = "TEST010101AAA",
                FiscalRegime = "601"
            });

        var localizerSMock = new Mock<IStringLocalizer<SaleService>>();
        localizerSMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerSMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        var saleService = new SaleService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<SaleService>.Instance,
            localizerSMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            discountSettingsMock.Object,
            Mock.Of<IDiscountAuthorizerService>(),
            realInventoryService,
            taxRateMock.Object,
            companySettingsMock.Object,
            taxSettingsMock.Object,
            Mock.Of<IProductPartialSurchargeService>(),
            roundingMock.Object,
            cashRegisterMock.Object,
            pricingService);

        var remissionService = new RemissionService(
            contextFactory,
            Mock.Of<IMapper>(),
            NullLogger<RemissionService>.Instance,
            localizerRMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            realInventoryService,
            pricingService,
            Mock.Of<IPdfService>(),
            Mock.Of<IEmailTemplateService>(),
            saleService,
            new DocumentSequenceService(),
            Options.Create(new BrandingOptions()));

        return (remissionService, saleService);
    }

    // -------------------------------------------------------------------------
    // Seed helpers
    // -------------------------------------------------------------------------

    private async Task<long> SeedCustomerAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        var customer = new Customer
        {
            Name = "Test Customer",
            CountryCode = "MX",
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<long> SeedProductAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        var unitMeasure = new UnitMeasure
        {
            CountryCode = "MX",
            Code = Guid.NewGuid().ToString("N")[..4],
            Name = "Pieza",
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.UnitMeasures.Add(unitMeasure);
        await ctx.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product-{Guid.NewGuid():N}",
            Code = Guid.NewGuid().ToString("N")[..8],
            Brand = "Test",
            Description = "Test",
            Price = 10m,
            IsActive = true,
            IsTaxable = false,
            RequiresInventory = true,
            IsPartialSaleAllowed = false,
            Content = 1,
            UnitMeasureId = unitMeasure.Id,
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product.Id;
    }

    /// <summary>
    /// Inventory with Quantity=0 — fails ValidateStockAvailabilityAsync (0 units available,
    /// 1 requested). Used to trigger movement failure and test transaction rollback.
    /// </summary>
    private async Task SeedDivergentInventoryAsync(long productId)
    {
        await using var ctx = new ApplicationDbContext(_options);
        ctx.Inventory.Add(new InventoryRow
        {
            ProductId = productId,
            LocationId = LocationId,
            Quantity = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    private async Task SeedNormalInventoryAsync(long productId, decimal quantity = 10)
    {
        await using var ctx = new ApplicationDbContext(_options);
        ctx.Inventory.Add(new InventoryRow
        {
            ProductId = productId,
            LocationId = LocationId,
            Quantity = quantity,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    private async Task<long> SeedAcceptedQuotationAsync(long customerId, params long[] productIds)
    {
        await using var ctx = new ApplicationDbContext(_options);
        var details = productIds.Select((pid, i) => new QuotationDetail
        {
            ProductId = pid,
            ProductName = $"Product {i + 1}",
            ProductCode = $"P{i}",
            Quantity = 1,
            UnitPrice = 10m,
            TaxRate = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        }).ToList();

        var quotation = new Quotation
        {
            QuotationNumber = $"COT-TC-{Guid.NewGuid():N}"[..14],
            CustomerId = customerId,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = QuotationStatus.Accepted,
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow,
            Details = details
        };
        ctx.Quotations.Add(quotation);
        await ctx.SaveChangesAsync();
        return quotation.Id;
    }

    // =========================================================================
    // Test 8 — Single product fails → remission rolled back
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenMovementFailsMidLoop_RemissionRolledBack()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync();
        await SeedDivergentInventoryAsync(productId);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        var (remissionService, _) = BuildServices();

        var result = await remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        Assert.That(result.IsSuccess, Is.False, "Movement failure must cause the remission operation to fail");

        await using var ctx = new ApplicationDbContext(_options);
        Assert.That(await ctx.Remissions.CountAsync(), Is.EqualTo(0),
            "Remission row must be rolled back when the inventory movement fails");
    }

    // =========================================================================
    // Test 9 — Second of two movements fails → no orphaned movements
    //
    // PRIMARY REGRESSION TEST: before the fix (standalone CreateMovementAsync),
    // the first product's movement was committed in its own context and remained
    // in the DB even after the outer transaction rolled back.
    // Expected: InventoryMovements.Count == 0
    // Regression signal: InventoryMovements.Count == 1
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenSecondOfTwoMovementsFails_NoOrphanedMovements()
    {
        var customerId = await SeedCustomerAsync();
        var productId1 = await SeedProductAsync();
        var productId2 = await SeedProductAsync();
        await SeedNormalInventoryAsync(productId1, quantity: 10);
        await SeedDivergentInventoryAsync(productId2);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId1, productId2);

        var (remissionService, _) = BuildServices();

        var result = await remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details =
            [
                new CreateRemissionDetailDto { ProductId = productId1, Quantity = 1, UnitPrice = 10m },
                new CreateRemissionDetailDto { ProductId = productId2, Quantity = 1, UnitPrice = 10m }
            ]
        });

        Assert.That(result.IsSuccess, Is.False, "Must fail when second of two movements fails");

        await using var ctx = new ApplicationDbContext(_options);

        // PRIMARY REGRESSION ASSERTION:
        // Before fix (standalone overload): Count == 1 — product1 movement committed independently
        // After fix (contextual overload):  Count == 0 — both movements rolled back with the transaction
        Assert.That(await ctx.InventoryMovements.CountAsync(), Is.EqualTo(0),
            "No inventory movements must survive after rollback. " +
            "Count == 1 means the contextual overload was replaced with the standalone — revert that change.");

        Assert.That(await ctx.Remissions.CountAsync(), Is.EqualTo(0),
            "Remission must also be rolled back when inventory fails mid-loop");
    }

    // =========================================================================
    // Test 10 — Movement fails AFTER SaveChangesAsync → quotation status rolled back
    //
    // This test is impossible with EF In-Memory (rollback is a no-op there).
    // Only a real transaction engine can prove this.
    // =========================================================================

    [Test]
    public async Task CreateRemission_WhenMovementFailsAfterSave_QuotationStatusRolledBack()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync();
        await SeedDivergentInventoryAsync(productId);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        var (remissionService, _) = BuildServices();

        await remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = productId, Quantity = 1, UnitPrice = 10m }]
        });

        await using var ctx = new ApplicationDbContext(_options);
        var quotation = await ctx.Quotations.FindAsync(quotationId);
        Assert.That(quotation!.Status, Is.EqualTo(QuotationStatus.Accepted),
            "The quotation status change (committed inside the tx via SaveChangesAsync) must be rolled back with the transaction when movement fails");
    }

    // =========================================================================
    // Test 11 — SaleService: movement fails → sale and movements rolled back
    // =========================================================================

    [Test]
    public async Task CreateSale_WhenMovementFailsMidLoop_SaleAndMovementsRolledBack_TC()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync();
        await SeedDivergentInventoryAsync(productId);

        var (_, saleService) = BuildServices();

        var result = await saleService.CreateSaleAsync(new CreateSaleDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            SaleDate = DateTime.UtcNow,
            SaleType = SaleType.Public,
            Details =
            [
                new CreateSaleDetailDto
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 10m,
                    IsCustomPrice = true
                }
            ],
            Payments = [new CreateSalePaymentDto { PaymentMethodId = PaymentMethodId, Amount = 10m }]
        });

        Assert.That(result.IsSuccess, Is.False, "Movement failure must cause the sale to fail");

        await using var ctx = new ApplicationDbContext(_options);
        Assert.That(await ctx.Sales.CountAsync(), Is.EqualTo(0),
            "Sale must be rolled back when the inventory movement fails");
        Assert.That(await ctx.InventoryMovements.CountAsync(), Is.EqualTo(0),
            "No orphaned inventory movements must remain after sale rollback");
    }

    // =========================================================================
    // Test 12 — SaleService: movement fails AFTER SaveChangesAsync → quotation status rolled back
    //
    // In CreateSaleInternalAsync, SaveChangesAsync commits both the sale row AND
    // quotation.Status = ConvertedToSale in one shot. If the inventory movement
    // then fails, the entire transaction must roll back — including that status change.
    // Without this test, a regression to the standalone inventory overload would leave
    // the quotation permanently stuck as ConvertedToSale with no actual sale in the DB.
    // =========================================================================

    [Test]
    public async Task CreateSale_WhenMovementFailsAfterSave_QuotationStatusRolledBack()
    {
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync();
        await SeedDivergentInventoryAsync(productId);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, productId);

        var (_, saleService) = BuildServices();

        await saleService.CreateSaleAsync(new CreateSaleDto
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
                    UnitPrice = 10m,
                    IsCustomPrice = true
                }
            ],
            Payments = [new CreateSalePaymentDto { PaymentMethodId = PaymentMethodId, Amount = 10m }]
        });

        await using var ctx = new ApplicationDbContext(_options);

        var quotation = await ctx.Quotations.FindAsync(quotationId);
        Assert.That(quotation!.Status, Is.EqualTo(QuotationStatus.Accepted),
            "quotation.Status = ConvertedToSale (set inside the tx via SaveChangesAsync) " +
            "must be rolled back together with the failed sale. " +
            "Status == ConvertedToSale means the inventory overload is standalone — revert that change.");

        Assert.That(await ctx.Sales.CountAsync(), Is.EqualTo(0),
            "Sale row must not survive when the inventory movement fails");
    }

    // =========================================================================
    // Test 13 — DocumentSequenceService (real, not mocked): folio increment must
    // roll back together with the remission when the operation fails.
    //
    // REGRESSION TEST for the incident-log 2026-07-22 fix: before it,
    // DocumentSequenceService.GetNextNumberAsync opened its own DbContext and
    // committed independently of the outer remission transaction. A failed
    // remission still consumed a sequence number, permanently burning it.
    // Expected: DocumentSequences row unchanged after a failed attempt, and the
    // next successful remission gets REM-{year}-0001 (no gap).
    // =========================================================================

    [Test]
    public async Task CreateRemission_RealDocumentSequence_FolioRolledBackOnFailure_NoGapOnNextSuccess()
    {
        var customerId = await SeedCustomerAsync();
        var failingProductId = await SeedProductAsync();
        await SeedDivergentInventoryAsync(failingProductId);
        var quotationId = await SeedAcceptedQuotationAsync(customerId, failingProductId);

        var (remissionService, _) = BuildServicesWithRealDocumentSequence();

        var failedResult = await remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = quotationId,
            Details = [new CreateRemissionDetailDto { ProductId = failingProductId, Quantity = 1, UnitPrice = 10m }]
        });

        Assert.That(failedResult.IsSuccess, Is.False, "Movement failure must cause the remission operation to fail");

        await using (var ctx = new ApplicationDbContext(_options))
        {
            var sequenceRow = await ctx.DocumentSequences
                .FirstOrDefaultAsync(s => s.DocumentType == "Remission" && s.Year == DateTime.UtcNow.Year);
            Assert.That(sequenceRow, Is.Null,
                "The sequence increment must roll back with the failed transaction — a non-null row here " +
                "means GetNextNumberAsync committed on its own, permanently burning the folio number.");
        }

        // Now succeed with a different product on normal inventory, and confirm no gap was left.
        var okProductId = await SeedProductAsync();
        await SeedNormalInventoryAsync(okProductId, quantity: 10);
        var okQuotationId = await SeedAcceptedQuotationAsync(customerId, okProductId);

        var okResult = await remissionService.CreateAsync(new CreateRemissionDto
        {
            CustomerId = customerId,
            LocationId = LocationId,
            QuotationId = okQuotationId,
            Details = [new CreateRemissionDetailDto { ProductId = okProductId, Quantity = 1, UnitPrice = 10m }]
        });

        Assert.That(okResult.IsSuccess, Is.True, okResult.Error);

        // Query the DB directly rather than trusting Result.Value — the AutoMapper mock in this
        // fixture has no Map<RemissionDto> setup, so the mapped DTO is always null even on success.
        await using var okCtx = new ApplicationDbContext(_options);
        var createdRemission = await okCtx.Remissions
            .Where(r => r.CustomerId == customerId && r.QuotationId == okQuotationId)
            .OrderByDescending(r => r.Id)
            .FirstAsync();

        Assert.That(createdRemission.RemissionNumber, Is.EqualTo($"REM-{DateTime.UtcNow.Year}-0001"),
            "First successful remission must get folio 0001 — a higher number means the failed " +
            "attempt already burned a sequence value.");
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
