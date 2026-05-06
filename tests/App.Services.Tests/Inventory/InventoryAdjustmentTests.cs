using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Inventory;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

using ShopLocation = App.Models.Shop.Location;

namespace App.Services.Tests.Inventory;

/// <summary>
/// Validates inventory adjustment business rules:
///   - Adjustment with no change (delta = 0) is rejected with a clear message.
///   - Adjustment dialog receives raw inventory.Quantity, not IndividualUnits.
///   - Adjustment to zero is allowed (sets stock to 0).
///   - Standard adjustment increases/decreases stock correctly.
/// </summary>
[TestFixture]
public class InventoryAdjustmentTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private InventoryService _inventoryService = null!;
    private InventoryQueryService _queryService = null!;

    private const int LocationId = 1;
    private const long ProductId = 1;

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
        currentUserMock.Setup(u => u.UserId).Returns("test-user");
        currentUserMock.Setup(u => u.FullName).Returns("Test User");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var alertEmailMock = new Mock<IInventoryAlertEmailService>();

        _inventoryService = new InventoryService(
            contextFactory,
            new Mock<IMapper>().Object,
            NullLogger<InventoryService>.Instance,
            currentUserMock.Object,
            localizerMock.Object,
            dateTimeMock.Object,
            alertEmailMock.Object);

        _queryService = new InventoryQueryService(
            contextFactory,
            new Mock<IMapper>().Object,
            NullLogger<InventoryQueryService>.Instance);
    }

    // ── InventoryService: adjustment validation ──────────────────────────────

    [Test]
    public async Task CreateAdjustment_WhenNewQuantityEqualsCurrentStock_ReturnsFailure()
    {
        await SeedInventoryAsync(quantity: 4m);

        var dto = new CreateInventoryAdjustmentDto
        {
            ProductId = ProductId,
            LocationId = LocationId,
            NewQuantity = 4m,
            AdjustmentType = InventoryMovementSubType.PhysicalCount,
            Reason = "Test"
        };

        var result = await _inventoryService.CreateInventoryAdjustmentAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("same as current stock").IgnoreCase);
    }

    [Test]
    public async Task CreateAdjustment_WhenNewQuantityDiffers_Succeeds()
    {
        await SeedInventoryAsync(quantity: 4m);

        var dto = new CreateInventoryAdjustmentDto
        {
            ProductId = ProductId,
            LocationId = LocationId,
            NewQuantity = 6m,
            AdjustmentType = InventoryMovementSubType.PhysicalCount,
            Reason = "Test"
        };

        var result = await _inventoryService.CreateInventoryAdjustmentAsync(dto);

        Assert.That(result.Success, Is.True);

        await using var context = new ApplicationDbContext(_dbOptions);
        var inventory = await context.Inventory.FirstAsync(x => x.ProductId == ProductId);
        Assert.That(inventory.Quantity, Is.EqualTo(6m));
    }

    [Test]
    public async Task CreateAdjustment_ToZero_Succeeds()
    {
        await SeedInventoryAsync(quantity: 4m);

        var dto = new CreateInventoryAdjustmentDto
        {
            ProductId = ProductId,
            LocationId = LocationId,
            NewQuantity = 0m,
            AdjustmentType = InventoryMovementSubType.PhysicalCount,
            Reason = "Physical count: nothing found"
        };

        var result = await _inventoryService.CreateInventoryAdjustmentAsync(dto);

        Assert.That(result.Success, Is.True);

        await using var context = new ApplicationDbContext(_dbOptions);
        var inventory = await context.Inventory.FirstAsync(x => x.ProductId == ProductId);
        Assert.That(inventory.Quantity, Is.EqualTo(0m));
    }

    // ── InventoryQueryService: Quantity vs IndividualUnits mapping ───────────

    [Test]
    public async Task GetProductStock_Quantity_MapsToInventoryQuantity_NotIndividualUnits()
    {
        // Reproduces the bug: IndividualUnits=1 but Quantity=4 in DB.
        // The adjustment dialog must receive Quantity=4 so the delta is computed correctly.
        await SeedInventoryAsync(quantity: 4m);

        var stock = await _queryService.GetProductStockAsync(ProductId, LocationId);

        var locationStock = stock!.LocationStock.First(w => w.LocationId == LocationId);
        Assert.That(locationStock.Quantity, Is.EqualTo(4m),
            "Quantity must reflect inventory.Quantity (used by adjustment service for delta calculation)");
    }

    [Test]
    public async Task GetProductStock_IndividualUnits_ComputedFromQuantity_WhenNotPartialSale()
    {
        // Non-partial product: GetAvailableIndividualUnits() returns Quantity directly.
        await SeedInventoryAsync(quantity: 4m);

        var stock = await _queryService.GetProductStockAsync(ProductId, LocationId);

        var locationStock = stock!.LocationStock.First(w => w.LocationId == LocationId);
        Assert.That(locationStock.IndividualUnits, Is.EqualTo(4m),
            "For non-partial products IndividualUnits == Quantity (no content factor applied)");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task SeedInventoryAsync(decimal quantity)
    {
        await using var context = new ApplicationDbContext(_dbOptions);

        var now = DateTime.UtcNow;

        var unitMeasure = new UnitMeasure
        {
            Id = 1, Name = "Pieza", Code = "pza", CountryCode = "MX",
            IsDeleted = 0, CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        };
        context.UnitMeasures.Add(unitMeasure);

        var product = new Product
        {
            Id = ProductId,
            Name = "PAPEL HIGIENICO DALIA 200M",
            Code = "P0115",
            Brand = "Cleeny",
            Price = 10m,
            Cost = 5m,
            Content = 1m,
            IsPartialSaleAllowed = false,
            RequiresInventory = true,
            UnitMeasureId = 1,
            IsActive = true,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        };
        context.Products.Add(product);

        var location = new ShopLocation
        {
            Id = LocationId,
            Name = "Tienda 1",
            Type = App.Core.Enums.Shop.LocationType.Branch,
            IsActive = true,
            IsDeleted = 0,
            CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        };
        context.Locations.Add(location);

        context.Inventory.Add(new App.Models.Shop.Inventory
        {
            ProductId = ProductId,
            LocationId = LocationId,
            Quantity = quantity,
            Version = [0, 0, 0, 0, 0, 0, 0, 1],
            CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        });

        await context.SaveChangesAsync();
    }

    // ── InventoryDto: TotalContent computed property ─────────────────────────

    [Test]
    public void InventoryDto_TotalContent_IsQuantityTimesProductContent()
    {
        var dto = new InventoryDto { Quantity = 3m, ProductContent = 1m };
        Assert.That(dto.TotalContent, Is.EqualTo(3m));
    }

    [Test]
    public void InventoryDto_TotalContent_WithContentGreaterThanOne()
    {
        var dto = new InventoryDto { Quantity = 5m, ProductContent = 12m };
        Assert.That(dto.TotalContent, Is.EqualTo(60m));
    }

    [Test]
    public void InventoryDto_TotalContent_WhenQuantityIsZero_IsZero()
    {
        var dto = new InventoryDto { Quantity = 0m, ProductContent = 12m };
        Assert.That(dto.TotalContent, Is.EqualTo(0m));
    }

    // ── InventoryMovementDto: TotalContent computed property ─────────────────

    [Test]
    public void InventoryMovementDto_TotalContent_IsQuantityMovedTimesProductContent()
    {
        var dto = new InventoryMovementDto { Quantity = 20m, ProductContent = 1m };
        Assert.That(dto.TotalContent, Is.EqualTo(20m));
    }

    [Test]
    public void InventoryMovementDto_TotalContent_WithContentGreaterThanOne()
    {
        var dto = new InventoryMovementDto { Quantity = 3m, ProductContent = 12m };
        Assert.That(dto.TotalContent, Is.EqualTo(36m));
    }

    [Test]
    public void InventoryMovementDto_TotalContent_WhenQuantityIsZero_IsZero()
    {
        var dto = new InventoryMovementDto { Quantity = 0m, ProductContent = 12m };
        Assert.That(dto.TotalContent, Is.EqualTo(0m));
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
