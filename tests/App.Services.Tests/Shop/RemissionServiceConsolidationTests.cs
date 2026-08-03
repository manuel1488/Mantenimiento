using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Settings;
using App.Core.DTOs.Shop;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
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
/// Reproduces the production incident (remission REM-2026-0044) end to end: create a remission
/// whose per-line discount has more than 2 decimals of precision, then consolidate it into a
/// sale, and verify the sale's total reproduces the remission's frozen total exactly — the
/// customer already paid that amount and payment validation must not reject it by a cent.
/// </summary>
[TestFixture]
public class RemissionServiceConsolidationTests
{
    private RemissionService _remissionService = null!;
    private SaleService _saleService = null!;

    private static readonly IServiceProvider EfServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    private const int LocationId = 1;
    private const long CustomerId = 1;
    private const int PaymentMethodId = 1;
    private const string UserId = "test-user-id";

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(EfServiceProvider)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var contextFactory = new TestDbContextFactory(_dbOptions);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
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
        mapperMock.Setup(m => m.Map<RemissionDto>(It.IsAny<Remission>()))
            .Returns((Remission r) => new RemissionDto
            {
                Id = r.Id,
                RemissionNumber = r.RemissionNumber,
                Subtotal = r.Subtotal,
                DiscountAmount = r.DiscountAmount,
                TaxAmount = r.TaxAmount,
                Total = r.Total,
                Status = r.Status
            });

        var taxRateServiceMock = new Mock<ITaxRateService>();
        taxRateServiceMock
            .Setup(t => t.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.16m);

        var roundingSettingsServiceMock = new Mock<IRoundingSettingsService>();
        roundingSettingsServiceMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        var companySettingsServiceMock = new Mock<ICompanySettingsService>();
        companySettingsServiceMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto
            {
                Id = 1,
                CompanyName = "Test Company",
                CountryCode = "MX",
                CurrencyCode = "MXN",
                TimeZoneId = "America/Mexico_City"
            });

        var taxSettingsServiceMock = new Mock<ITaxSettingsService>();
        taxSettingsServiceMock.Setup(t => t.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                Id = 1,
                CountryCode = "MX",
                BusinessName = "Test",
                TaxId = "TEST000000AA0",
                FiscalRegime = "601"
            });

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync(UserId);
        currentUserServiceMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test User");
        currentUserServiceMock.Setup(u => u.GetActiveLocationIdAsync()).ReturnsAsync(LocationId);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var discountSettingsServiceMock = new Mock<IDiscountSettingsService>();
        discountSettingsServiceMock.Setup(d => d.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DiscountSettingsDto>.Success(new DiscountSettingsDto
            {
                Id = 1,
                MaximumPublicDiscount = 100,
                RequireAuthorizationForPublicDiscount = false
            }));

        var discountAuthorizerServiceMock = new Mock<IDiscountAuthorizerService>();

        var inventoryServiceMock = new Mock<IContextualInventoryService>();
        inventoryServiceMock
            .Setup(i => i.ValidateStockAvailabilityAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        inventoryServiceMock
            .Setup(i => i.CreateMovementAsync(It.IsAny<CreateInventoryMovementDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovementOperationResult { Success = true });
        inventoryServiceMock
            .Setup(i => i.CreateMovementAsync(It.IsAny<CreateInventoryMovementDto>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovementOperationResult { Success = true });

        var partialSurchargeServiceMock = new Mock<IProductPartialSurchargeService>();

        var cashRegisterServiceMock = new Mock<ICashRegisterService>();
        cashRegisterServiceMock
            .Setup(c => c.GetActiveCashRegisterAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(Result<CashRegisterDto?>.Success(new CashRegisterDto
            {
                Id = 1,
                LocationId = LocationId,
                UserId = UserId,
                Status = CashRegisterStatus.Open,
                ExpectedCash = 0
            }));
        cashRegisterServiceMock
            .Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(Result<CashRegisterSettingsDto>.Success(new CashRegisterSettingsDto
            {
                IsStrictCashLimit = false
            }));

        var pricingService = new PricingCalculationService(
            taxRateServiceMock.Object,
            companySettingsServiceMock.Object,
            roundingSettingsServiceMock.Object,
            NullLogger<PricingCalculationService>.Instance);

        var saleLocalizerMock = new Mock<IStringLocalizer<SaleService>>();
        saleLocalizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        saleLocalizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        _saleService = new SaleService(
            contextFactory,
            mapperMock.Object,
            NullLogger<SaleService>.Instance,
            saleLocalizerMock.Object,
            currentUserServiceMock.Object,
            dateTimeMock.Object,
            discountSettingsServiceMock.Object,
            discountAuthorizerServiceMock.Object,
            inventoryServiceMock.Object,
            taxRateServiceMock.Object,
            companySettingsServiceMock.Object,
            taxSettingsServiceMock.Object,
            partialSurchargeServiceMock.Object,
            roundingSettingsServiceMock.Object,
            cashRegisterServiceMock.Object,
            pricingService);

        var documentSequenceServiceMock = new Mock<IDocumentSequenceService>();
        documentSequenceServiceMock
            .Setup(d => d.GetNextNumberAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("REM-2026-0001");

        var pdfServiceMock = new Mock<IPdfService>();
        var emailTemplateServiceMock = new Mock<IEmailTemplateService>();

        var remissionLocalizerMock = new Mock<IStringLocalizer<RemissionService>>();
        remissionLocalizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        remissionLocalizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        _remissionService = new RemissionService(
            contextFactory,
            mapperMock.Object,
            NullLogger<RemissionService>.Instance,
            remissionLocalizerMock.Object,
            currentUserServiceMock.Object,
            dateTimeMock.Object,
            taxRateServiceMock.Object,
            companySettingsServiceMock.Object,
            inventoryServiceMock.Object,
            pricingService,
            pdfServiceMock.Object,
            emailTemplateServiceMock.Object,
            _saleService,
            documentSequenceServiceMock.Object);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var context = new ApplicationDbContext(_dbOptions);

        context.UnitMeasures.Add(new UnitMeasure
        {
            Id = 1,
            Code = "PZA",
            Name = "Pieza",
            CountryCode = "MX",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        context.Customers.Add(new Customer
        {
            Id = CustomerId,
            Name = "Test Customer",
            CountryCode = "MX",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        context.Locations.Add(new App.Models.Shop.Location
        {
            Id = LocationId,
            Name = "Tienda 1",
            Type = LocationType.Branch,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        context.PaymentMethods.Add(new App.Models.Settings.PaymentMethod
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

    private void SeedProduct(long id, decimal price, bool requiresInventory = false)
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
            IsTaxable = true,
            IsActive = true,
            UnitMeasureId = 1,
            Content = 1,
            IsPartialSaleAllowed = false,
            QuantityStep = 1,
            RequiresInventory = requiresInventory,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    [Test]
    public async Task CreateRemission_LineDiscountFromPercentage_PersistsFullPrecisionNotRoundedToCents()
    {
        // Regression: RemissionService.CreateAsync used to store each line's DiscountAmount
        // rounded to 2 decimals (Math.Round(lineCalc.DiscountAmount, 2)), discarding the
        // sub-cent precision the remission's own document-level total already depended on.
        // A 20% discount on 711.20691 (30 x 23.706897) yields a discount with more than
        // 2 decimals — the exact scenario that caused REM-2026-0044 to reproduce a total
        // $0.01 higher than the frozen amount when consolidated.
        SeedProduct(175, 23.706897m);

        var dto = new CreateRemissionDto
        {
            CustomerId = CustomerId,
            LocationId = LocationId,
            Details = new List<CreateRemissionDetailDto>
            {
                new() { ProductId = 175, Quantity = 30m, UnitPrice = 23.706897m, DiscountPercentage = 20m }
            }
        };

        var result = await _remissionService.CreateAsync(dto);

        Assert.That(result.IsSuccess, Is.True, $"Remission creation should succeed: {result.Error}");

        // Read back the persisted line directly to make sure the assertion isn't fooled by an
        // in-memory (untracked) value that never round-tripped through the DB column.
        await using var context = new ApplicationDbContext(_dbOptions);
        var persistedDetail = await context.RemissionDetails
            .FirstAsync(d => d.RemissionId == result.Value!.Id);

        // 30 * 23.706897 = 711.20691; 20% discount = 142.241382 -- NOT a round 2-decimal value.
        Assert.That(persistedDetail.DiscountAmount, Is.EqualTo(142.241382m),
            "Line discount must be persisted at full precision, not rounded to 2 decimals");
    }

    [Test]
    public async Task ConsolidateRemission_LineWithSubCentDiscountPrecision_SaleTotalMatchesFrozenRemissionTotal()
    {
        // End-to-end reproduction of the production incident: create a remission with a
        // discount that has sub-cent precision, consolidate it, and confirm the resulting
        // sale's total is exactly the remission's frozen total -- not a cent higher.
        SeedProduct(175, 23.706897m);

        var createDto = new CreateRemissionDto
        {
            CustomerId = CustomerId,
            LocationId = LocationId,
            Details = new List<CreateRemissionDetailDto>
            {
                new() { ProductId = 175, Quantity = 30m, UnitPrice = 23.706897m, DiscountPercentage = 20m }
            }
        };

        var createResult = await _remissionService.CreateAsync(createDto);
        Assert.That(createResult.IsSuccess, Is.True, $"Remission creation should succeed: {createResult.Error}");

        var frozenTotal = createResult.Value!.Total;

        var consolidateDto = new ConsolidateRemissionsDto
        {
            CustomerId = CustomerId,
            LocationId = LocationId,
            RemissionIds = new List<long> { createResult.Value!.Id },
            Payments = new List<CreateSalePaymentDto>
            {
                new() { PaymentMethodId = PaymentMethodId, Amount = frozenTotal }
            }
        };

        var consolidateResult = await _remissionService.ConsolidateAsync(consolidateDto);

        Assert.That(consolidateResult.IsSuccess, Is.True,
            $"Consolidating with the exact frozen amount paid must not fail: {consolidateResult.Error}");

        await using var context = new ApplicationDbContext(_dbOptions);
        var sale = await context.Sales.FirstAsync(s => s.Id == consolidateResult.Value);

        Assert.That(sale.Total, Is.EqualTo(frozenTotal),
            "Consolidated sale total must reproduce the remission's frozen total exactly");
    }

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext() => new(_options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
