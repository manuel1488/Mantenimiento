using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Constants;
using App.Core.DTOs.Inventory;
using App.Core.DTOs.Location;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Inventory;
using App.Services.Locations;
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
/// Validates that Inventory (transfers, adjustments, initial loads, movement/alert queries and
/// the location picker) is restricted to the locations a non-global-access user has assigned in
/// UserLocation — the gap reported: a user with only "Tienda 1" assigned could previously see and
/// operate on every warehouse.
/// </summary>
[TestFixture]
public class InventoryLocationAccessTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<ICurrentUserService> _currentUserMock = null!;
    private InventoryService _inventoryService = null!;
    private LocationService _locationService = null!;

    private const int AllowedLocationId = 1;
    private const int ForeignLocationId = 2;
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

        // Non-global user with only AllowedLocationId assigned in UserLocation.
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test User");
        _currentUserMock.Setup(u => u.GetIsGlobalAccessAsync()).ReturnsAsync(false);
        _currentUserMock.Setup(u => u.GetAssignedLocationIdsAsync())
            .ReturnsAsync(new List<int> { AllowedLocationId });
        _currentUserMock.Setup(u => u.HasAccessToLocationAsync(AllowedLocationId)).ReturnsAsync(true);
        _currentUserMock.Setup(u => u.HasAccessToLocationAsync(ForeignLocationId)).ReturnsAsync(false);

        var inventoryMapperMock = new Mock<IMapper>();
        inventoryMapperMock
            .Setup(m => m.Map<InventoryMovementDto>(It.IsAny<object>()))
            .Returns((object src) =>
            {
                var movement = (InventoryMovement)src;
                return new InventoryMovementDto
                {
                    ProductId = movement.ProductId,
                    LocationId = movement.LocationId,
                    DestinationLocationId = movement.DestinationLocationId
                };
            });
        inventoryMapperMock
            .Setup(m => m.Map<InventoryAlertDto>(It.IsAny<object>()))
            .Returns((object src) =>
            {
                var inventory = (App.Models.Shop.Inventory)src;
                return new InventoryAlertDto
                {
                    ProductId = inventory.ProductId,
                    LocationId = inventory.LocationId
                };
            });

        _inventoryService = new InventoryService(
            contextFactory,
            inventoryMapperMock.Object,
            NullLogger<InventoryService>.Instance,
            _currentUserMock.Object,
            localizerMock.Object,
            Mock.Of<IDateTime>(d => d.Now == DateTime.UtcNow),
            new Mock<IInventoryAlertEmailService>().Object,
            new Mock<ICompanySettingsService>().Object,
            new Mock<IPdfService>().Object,
            new Mock<IEmailTemplateService>().Object,
            new Mock<IDocumentSequenceService>().Object,
            Options.Create(new BrandingOptions()));

        var locationMapperMock = new Mock<IMapper>();
        locationMapperMock
            .Setup(m => m.Map<LocationDto>(It.IsAny<ShopLocation>()))
            .Returns((ShopLocation loc) => new LocationDto
            {
                Id = loc.Id,
                Name = loc.Name,
                Type = loc.Type,
                IsActive = loc.IsActive
            });

        _locationService = new LocationService(
            contextFactory,
            locationMapperMock.Object,
            NullLogger<LocationService>.Instance,
            new Mock<IStringLocalizer<LocationService>>().Object,
            _currentUserMock.Object,
            Mock.Of<IDateTime>(d => d.Now == DateTime.UtcNow));
    }

    // ── InventoryService: write guards ────────────────────────────────────────

    [Test]
    public async Task CreateTransfer_WhenNoAccessToSourceLocation_ReturnsFailure()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(ForeignLocationId, quantity: 10m);

        var result = await _inventoryService.CreateTransferAsync(new CreateInventoryTransferDto
        {
            ProductId = ProductId,
            LocationId = ForeignLocationId,
            DestinationLocationId = AllowedLocationId,
            Quantity = 1m,
            Reason = "Test"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("access").IgnoreCase);
    }

    [Test]
    public async Task CreateTransfer_WhenNoAccessToDestinationLocation_ReturnsFailure()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(AllowedLocationId, quantity: 10m);

        var result = await _inventoryService.CreateTransferAsync(new CreateInventoryTransferDto
        {
            ProductId = ProductId,
            LocationId = AllowedLocationId,
            DestinationLocationId = ForeignLocationId,
            Quantity = 1m,
            Reason = "Test"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("access").IgnoreCase);
    }

    [Test]
    public async Task CreateTransfer_WhenNoAccessToEitherLocation_DoesNotMutateStock()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(ForeignLocationId, quantity: 10m);

        await _inventoryService.CreateTransferAsync(new CreateInventoryTransferDto
        {
            ProductId = ProductId,
            LocationId = ForeignLocationId,
            DestinationLocationId = AllowedLocationId,
            Quantity = 5m,
            Reason = "Test"
        });

        await using var context = new ApplicationDbContext(_dbOptions);
        var sourceInventory = await context.Inventory.FirstAsync(x => x.LocationId == ForeignLocationId);
        Assert.That(sourceInventory.Quantity, Is.EqualTo(10m), "Denied transfer must not touch the database");
        Assert.That(await context.InventoryMovements.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateInventoryAdjustment_WhenNoLocationAccess_ReturnsFailure()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(ForeignLocationId, quantity: 4m);

        var result = await _inventoryService.CreateInventoryAdjustmentAsync(new CreateInventoryAdjustmentDto
        {
            ProductId = ProductId,
            LocationId = ForeignLocationId,
            NewQuantity = 6m,
            AdjustmentType = InventoryMovementSubType.PhysicalCount,
            Reason = "Test"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("access").IgnoreCase);
    }

    [Test]
    public async Task CreateInitialInventory_WhenNoLocationAccess_ReturnsError()
    {
        await SeedProductAndLocationsAsync();

        var result = await _inventoryService.CreateInitialInventoryAsync(new InitialInventoryLoadDto
        {
            ProductId = ProductId,
            LocationId = ForeignLocationId,
            Quantity = 10m,
            Reason = "Test"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("access").IgnoreCase);
    }

    // ── InventoryService: read filters ────────────────────────────────────────

    [Test]
    public async Task GetMovements_NonGlobalUser_NoLocationFilter_OnlyReturnsAssignedLocationMovements()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(AllowedLocationId, quantity: 10m);
        await SeedInventoryAsync(ForeignLocationId, quantity: 10m);
        await SeedMovementAsync(AllowedLocationId);
        await SeedMovementAsync(ForeignLocationId);

        var (totalCount, items) = await _inventoryService.GetMovementsAsync();

        Assert.That(totalCount, Is.EqualTo(1));
        Assert.That(items.Single().LocationId, Is.EqualTo(AllowedLocationId));
    }

    [Test]
    public async Task GetMovements_NonGlobalUser_RequestsForeignLocation_ReturnsEmpty()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(ForeignLocationId, quantity: 10m);
        await SeedMovementAsync(ForeignLocationId);

        var (totalCount, items) = await _inventoryService.GetMovementsAsync(locationId: ForeignLocationId);

        Assert.That(totalCount, Is.EqualTo(0));
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task GetStockAlerts_NonGlobalUser_RequestsForeignLocation_ReturnsEmpty()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(ForeignLocationId, quantity: 0m, minStock: 5m);

        var alerts = await _inventoryService.GetStockAlertsAsync(locationId: ForeignLocationId);

        Assert.That(alerts, Is.Empty);
    }

    [Test]
    public async Task GetStockAlerts_NonGlobalUser_NoLocationFilter_OnlyReturnsAssignedLocationAlerts()
    {
        await SeedProductAndLocationsAsync();
        await SeedInventoryAsync(AllowedLocationId, quantity: 0m, minStock: 5m);
        await SeedInventoryAsync(ForeignLocationId, quantity: 0m, minStock: 5m);

        var alerts = await _inventoryService.GetStockAlertsAsync();

        Assert.That(alerts.Select(a => a.LocationId), Is.EquivalentTo(new[] { AllowedLocationId }));
    }

    // ── LocationService: location picker ──────────────────────────────────────

    [Test]
    public async Task GetAccessibleLocations_GlobalAccessUser_ReturnsAllActiveLocations()
    {
        await SeedProductAndLocationsAsync();
        _currentUserMock.Setup(u => u.GetIsGlobalAccessAsync()).ReturnsAsync(true);

        var locations = await _locationService.GetAccessibleLocationsAsync();

        Assert.That(locations.Select(l => l.Id), Is.EquivalentTo(new[] { AllowedLocationId, ForeignLocationId }));
    }

    [Test]
    public async Task GetAccessibleLocations_NonGlobalUser_ReturnsOnlyAssignedLocations()
    {
        await SeedProductAndLocationsAsync();

        var locations = await _locationService.GetAccessibleLocationsAsync();

        Assert.That(locations.Select(l => l.Id), Is.EquivalentTo(new[] { AllowedLocationId }));
    }

    [Test]
    public async Task GetAccessibleLocations_NonGlobalUser_WithNoAssignments_ReturnsEmpty()
    {
        await SeedProductAndLocationsAsync();
        _currentUserMock.Setup(u => u.GetAssignedLocationIdsAsync()).ReturnsAsync(new List<int>());

        var locations = await _locationService.GetAccessibleLocationsAsync();

        Assert.That(locations, Is.Empty);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task SeedProductAndLocationsAsync()
    {
        await using var context = new ApplicationDbContext(_dbOptions);

        var now = DateTime.UtcNow;

        if (!await context.UnitMeasures.AnyAsync())
        {
            context.UnitMeasures.Add(new UnitMeasure
            {
                Id = 1, Name = "Pieza", Code = "pza", CountryCode = "MX",
                IsDeleted = 0, CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
            });
        }

        if (!await context.Products.AnyAsync(p => p.Id == ProductId))
        {
            context.Products.Add(new Product
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
            });
        }

        foreach (var (id, name) in new[] { (AllowedLocationId, "Tienda 1"), (ForeignLocationId, "Tienda 2") })
        {
            if (!await context.Locations.AnyAsync(l => l.Id == id))
            {
                context.Locations.Add(new ShopLocation
                {
                    Id = id,
                    Name = name,
                    Type = LocationType.Branch,
                    IsActive = true,
                    IsDeleted = 0,
                    CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedInventoryAsync(int locationId, decimal quantity, decimal? minStock = null)
    {
        await using var context = new ApplicationDbContext(_dbOptions);

        var now = DateTime.UtcNow;
        context.Inventory.Add(new App.Models.Shop.Inventory
        {
            ProductId = ProductId,
            LocationId = locationId,
            Quantity = quantity,
            MinStock = minStock,
            Version = [0, 0, 0, 0, 0, 0, 0, 1],
            CreatedBy = "seed", CreatedAt = now, ModifiedBy = "seed", ModifiedAt = now
        });

        await context.SaveChangesAsync();
    }

    private async Task SeedMovementAsync(int locationId)
    {
        await using var context = new ApplicationDbContext(_dbOptions);

        var now = DateTime.UtcNow;
        context.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = ProductId,
            LocationId = locationId,
            MovementType = InventoryMovementType.StockIn,
            MovementSubType = InventoryMovementSubType.InitialCount,
            Quantity = 1m,
            PreviousBalance = 0m,
            NewBalance = 1m,
            MovementDate = now,
            Reason = "Test",
            CreatedBy = "seed", CreatedAt = now
        });

        await context.SaveChangesAsync();
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
