using Moq;
using NUnit.Framework;

using App.Core.DTOs.Shop;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Tests for the wholesale-price write gate: a wholesale FixedPrice must be below the
/// product's retail price. A price >= retail would yield a negative discount (surcharge)
/// that corrupts downstream totals (the root cause of the COT-2026-0132 conversion bug).
/// </summary>
[TestFixture]
public class ProductWholesalePriceValidationTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private ProductWholesalePriceService _service = null!;
    private const long ProductId = 1;
    private const decimal RetailPrice = 43.103448m;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test User");

        var dateMock = new Mock<IDateTime>();
        dateMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var localizerMock = new Mock<IStringLocalizer<ProductWholesalePriceService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        _service = new ProductWholesalePriceService(
            new TestDbContextFactory(_dbOptions),
            new Mock<AutoMapper.IMapper>().Object,
            NullLogger<ProductWholesalePriceService>.Instance,
            localizerMock.Object,
            userMock.Object,
            dateMock.Object);

        using var context = new ApplicationDbContext(_dbOptions);
        context.Products.Add(new Product
        {
            Id = ProductId, Code = "P0001", Name = "Test", Brand = "Test",
            Price = RetailPrice, Cost = 0, IsTaxable = true, IsActive = true,
            UnitMeasureId = 1, Content = 1, QuantityStep = 1,
            RequiresInventory = false, CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private UpdateProductWholesalePricesDto Dto(decimal? fixedPrice, bool isActive) => new()
    {
        ProductId = ProductId,
        WholesalePrices =
        [
            new CreateProductWholesalePriceDto
            {
                WholesaleTierId = 1, MinQuantity = 20m,
                DiscountPercentage = 0, FixedPrice = fixedPrice, IsActive = isActive
            }
        ]
    };

    [Test]
    public async Task Update_FixedPriceBelowRetail_Succeeds()
    {
        var result = await _service.UpdateProductWholesalePricesAsync(Dto(40.0m, isActive: true));

        Assert.That(result.IsSuccess, Is.True, $"Should accept a wholesale price below retail: {result.Error}");
    }

    [Test]
    public async Task Update_ActiveFixedPriceAboveRetail_Fails()
    {
        var result = await _service.UpdateProductWholesalePricesAsync(Dto(50.0m, isActive: true));

        Assert.That(result.IsSuccess, Is.False, "Should reject a wholesale price above retail");
        Assert.That(result.Error, Does.Contain("wholesale price").Or.Contain("mayoreo"));
    }

    [Test]
    public async Task Update_ActiveFixedPriceEqualToRetail_Fails()
    {
        var result = await _service.UpdateProductWholesalePricesAsync(Dto(RetailPrice, isActive: true));

        Assert.That(result.IsSuccess, Is.False, "A wholesale price equal to retail yields a zero/negative discount and must be rejected");
    }

    [Test]
    public async Task Update_InactiveFixedPriceAboveRetail_Succeeds()
    {
        // An inactive tier is never applied, so the user can keep/deactivate a bad row while fixing it.
        var result = await _service.UpdateProductWholesalePricesAsync(Dto(50.0m, isActive: false));

        Assert.That(result.IsSuccess, Is.True, $"Inactive tiers are exempt: {result.Error}");
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
