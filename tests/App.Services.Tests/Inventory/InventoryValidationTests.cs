using Moq;
using NUnit.Framework;

using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Inventory;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ShopLocation = App.Models.Shop.Location;

namespace App.Services.Tests.Inventory;

/// <summary>
/// Validates stock availability logic in <see cref="InventoryService.ValidateStockAvailabilityAsync"/>.
///
/// Core invariant: available stock is always derived from <c>Inventory.Quantity × Product.Content</c>
/// (via <c>GetAvailableIndividualUnits()</c>). No stored derived field is trusted.
///
/// Regression coverage: P0175 — product with sufficient Quantity was blocked because
/// a stale <c>IndividualUnits</c> field was read instead of computing from Quantity.
/// </summary>
[TestFixture]
public class InventoryValidationTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private InventoryService _inventoryService = null!;

    private const int LocationId = 1;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var contextFactory = new TestDbContextFactory(_dbOptions);

        var localizerMock = new Mock<IStringLocalizer<InventoryService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test User");

        _inventoryService = new InventoryService(
            contextFactory,
            new Moq.Mock<AutoMapper.IMapper>().Object,
            NullLogger<InventoryService>.Instance,
            currentUserMock.Object,
            localizerMock.Object,
            new Mock<IDateTime>().Object,
            new Mock<IInventoryAlertEmailService>().Object,
            new Mock<ICompanySettingsService>().Object,
            new Mock<IPdfService>().Object,
            new Mock<IEmailTemplateService>().Object,
            new Mock<IDocumentSequenceService>().Object,
            Options.Create(new BrandingOptions()));
    }

    // =========================================================================
    // Non-partial products — available stock == Quantity
    // =========================================================================

    [Test]
    public async Task Validate_NonPartial_ExactQuantity_ReturnsTrue()
    {
        var productId = await SeedInventoryAsync(quantity: 5m, isPartialSale: false, content: 1m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 5m);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task Validate_NonPartial_QuantityExceeds_ReturnsFalse()
    {
        var productId = await SeedInventoryAsync(quantity: 5m, isPartialSale: false, content: 1m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 6m);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task Validate_NonPartial_ZeroStock_ReturnsFalse()
    {
        var productId = await SeedInventoryAsync(quantity: 0m, isPartialSale: false, content: 1m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 1m);

        Assert.That(result, Is.False);
    }

    // =========================================================================
    // Partial-sale products with Content = 1 (1 container = 1 individual unit)
    // Regression: P0175 — MULTICLEAN LIMPIADOR, Quantity=80, Content=1, requested=60
    // Previously failed because stale IndividualUnits=40.20 was read from DB.
    // =========================================================================

    [Test]
    public async Task Validate_PartialSale_Content1_SufficientQuantity_ReturnsTrue_P0175Regression()
    {
        // Exact bug scenario: Quantity=80 (correct) but old code read IndividualUnits=40.20 (stale)
        // causing false rejection for a 60-unit request.
        var productId = await SeedInventoryAsync(quantity: 80m, isPartialSale: true, content: 1m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 60m);

        Assert.That(result, Is.True,
            "Must pass: 80 units available, 60 requested. " +
            "Old code failed here by reading a stale IndividualUnits field.");
    }

    [Test]
    public async Task Validate_PartialSale_Content1_ExactQuantity_ReturnsTrue()
    {
        var productId = await SeedInventoryAsync(quantity: 80m, isPartialSale: true, content: 1m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 80m);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task Validate_PartialSale_Content1_ExceedsQuantity_ReturnsFalse()
    {
        var productId = await SeedInventoryAsync(quantity: 80m, isPartialSale: true, content: 1m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 80.01m);

        Assert.That(result, Is.False);
    }

    // =========================================================================
    // Partial-sale products with Content > 1 (e.g. 19 liters per container)
    // Available = Quantity × Content
    // =========================================================================

    [Test]
    public async Task Validate_PartialSale_ContentGreaterThanOne_SufficientStock_ReturnsTrue()
    {
        // 10 containers × 19 liters = 190 liters available; request 150 liters
        var productId = await SeedInventoryAsync(quantity: 10m, isPartialSale: true, content: 19m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 150m);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task Validate_PartialSale_ContentGreaterThanOne_ExactTotalContent_ReturnsTrue()
    {
        // 10 × 19 = 190 liters; request exactly 190
        var productId = await SeedInventoryAsync(quantity: 10m, isPartialSale: true, content: 19m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 190m);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task Validate_PartialSale_ContentGreaterThanOne_ExceedsTotalContent_ReturnsFalse()
    {
        // 10 × 19 = 190 liters; request 191 liters
        var productId = await SeedInventoryAsync(quantity: 10m, isPartialSale: true, content: 19m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 191m);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task Validate_PartialSale_ContentGreaterThanOne_ZeroContainers_ReturnsFalse()
    {
        var productId = await SeedInventoryAsync(quantity: 0m, isPartialSale: true, content: 19m);

        var result = await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 1m);

        Assert.That(result, Is.False);
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Test]
    public async Task Validate_ProductNotInInventory_ReturnsFalse()
    {
        // Seed a different product so the location/product combination is missing
        await SeedInventoryAsync(quantity: 100m, isPartialSale: false, content: 1m);
        var missingProductId = 99999L;

        var result = await _inventoryService.ValidateStockAvailabilityAsync(missingProductId, LocationId, 1m);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task Validate_PartialSale_ContentIsZero_FallsBackToQuantity()
    {
        // Degenerate product: IsPartialSaleAllowed=true but Content=0 (misconfigured).
        // GetAvailableIndividualUnits falls back to Quantity.
        var productId = await SeedInventoryAsync(quantity: 5m, isPartialSale: true, content: 0m);

        Assert.That(await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 5m), Is.True);
        Assert.That(await _inventoryService.ValidateStockAvailabilityAsync(productId, LocationId, 6m), Is.False);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<long> SeedInventoryAsync(decimal quantity, bool isPartialSale, decimal content)
    {
        await using var context = new ApplicationDbContext(_dbOptions);
        var now = DateTime.UtcNow;

        var unitMeasure = new UnitMeasure
        {
            Name = "Pieza", Code = Guid.NewGuid().ToString("N")[..4], CountryCode = "MX",
            IsDeleted = 0, CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        };
        context.UnitMeasures.Add(unitMeasure);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product-{Guid.NewGuid():N}",
            Code = Guid.NewGuid().ToString("N")[..8],
            Brand = "Test",
            Description = "Test",
            Price = 10m,
            Content = content,
            IsPartialSaleAllowed = isPartialSale,
            RequiresInventory = true,
            UnitMeasureId = unitMeasure.Id,
            IsActive = true,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        if (!await context.Locations.AnyAsync(l => l.Id == LocationId))
        {
            context.Locations.Add(new ShopLocation
            {
                Id = LocationId, Name = "Tienda 1",
                Type = App.Core.Enums.Shop.LocationType.Branch,
                IsActive = true, IsDeleted = 0,
                CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
            });
        }

        context.Inventory.Add(new App.Models.Shop.Inventory
        {
            ProductId = product.Id,
            LocationId = LocationId,
            Quantity = quantity,
            Version = [0, 0, 0, 0, 0, 0, 0, 1],
            CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        });

        await context.SaveChangesAsync();
        return product.Id;
    }

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new(_options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken _ = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
