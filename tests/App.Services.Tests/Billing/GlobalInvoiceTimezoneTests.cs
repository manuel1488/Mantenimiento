using Moq;
using NUnit.Framework;

using App.Core.DTOs.Billing.Mexico;
using App.Core.DTOs.Settings;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Constants;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Services.Settings;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Billing;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Billing;

/// <summary>
/// Tests that GlobalInvoiceService.PreviewAsync correctly converts local calendar dates
/// to UTC when querying SaleDate (which is always stored in UTC).
///
/// Context: A sale made at 22:00 local (UTC-6) is stored as 04:00 UTC next day.
/// If the filter uses the raw local date 00:00–23:59, that sale falls outside the range.
/// The fix converts local midnight boundaries to UTC before querying.
///
/// Timezone used: America/Mexico_City (UTC-6 winter / UTC-5 summer = CDT).
/// </summary>
[TestFixture]
public class GlobalInvoiceTimezoneTests
{
    // America/Mexico_City: UTC-6 standard (winter), UTC-5 DST (summer ~April-Oct)
    private static readonly TimeZoneInfo MexicoCityTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<ICompanySettingsService> _companySvcMock = null!;
    private GlobalInvoiceService _service = null!;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .Options;

        _companySvcMock = new Mock<ICompanySettingsService>();
        _companySvcMock.Setup(c => c.GetCurrentTimeZoneAsync())
            .ReturnsAsync(MexicoCityTz);

        _service = BuildService();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Daily period — UTC-6 (winter)
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public async Task DailyPreview_SaleAtMidnightLocal_IsIncluded()
    {
        // 01/Apr/2026 00:00 local (UTC-6) = 01/Apr/2026 06:00 UTC
        var saleUtc = LocalToUtc(2026, 4, 1, 0, 0, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(1));
    }

    [Test]
    public async Task DailyPreview_SaleAt2359Local_IsIncluded()
    {
        // 01/Apr/2026 23:59 local (UTC-6) = 02/Apr/2026 05:59 UTC
        var saleUtc = LocalToUtc(2026, 4, 1, 23, 59, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(1),
            "Sale at 23:59 local must be included even though it falls on UTC next day.");
    }

    [Test]
    public async Task DailyPreview_SaleAt2100Local_IsIncluded()
    {
        // 01/Apr/2026 21:00 local (UTC-6) = 02/Apr/2026 03:00 UTC
        var saleUtc = LocalToUtc(2026, 4, 1, 21, 0, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(1),
            "Sale at 21:00 local crosses UTC midnight but belongs to local 01/Apr.");
    }

    [Test]
    public async Task DailyPreview_SaleFromPreviousDayLocal_IsExcluded()
    {
        // 31/Mar/2026 23:00 local (UTC-6) = 01/Apr/2026 05:00 UTC
        // This sale is UTC 05:00 on 01/Apr but locally it happened on 31/Mar — must be excluded
        var saleUtc = LocalToUtc(2026, 3, 31, 23, 0, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(0),
            "Sale made locally on 31/Mar must not appear in the 01/Apr filter.");
    }

    [Test]
    public async Task DailyPreview_SaleExactlyAtLocalMidnightBoundary_IsExcluded()
    {
        // 02/Apr/2026 00:00 local = 02/Apr/2026 06:00 UTC — next day, should be excluded
        var saleUtc = LocalToUtc(2026, 4, 2, 0, 0, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(0),
            "Sale at 00:00 local on 02/Apr must not appear in the 01/Apr filter.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Monthly period — boundary days
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public async Task MonthlyPreview_SaleAtLastDayMidnightLocal_IsIncluded()
    {
        // 30/Apr/2026 23:30 local (CDT = UTC-5 in April) = 01/May/2026 04:30 UTC
        var saleUtc = LocalToUtc(2026, 4, 30, 23, 30, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(1),
            "Sale at 23:30 on last day of April (CDT) must be included in the April monthly filter.");
    }

    [Test]
    public async Task MonthlyPreview_SaleFromNextMonthLocal_IsExcluded()
    {
        // 01/May/2026 00:00 local (CDT = UTC-5) = 01/May/2026 05:00 UTC
        var saleUtc = LocalToUtc(2026, 5, 1, 0, 0, MexicoCityTz);
        await SeedSaleAsync(saleUtc);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(0),
            "Sale on 01/May local must not appear in the April monthly filter.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Multiple sales — split across UTC midnight
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public async Task DailyPreview_MultipleSalesAcrossUtcMidnight_AllIncluded()
    {
        // Three sales on 01/Apr local at different hours, two of which cross UTC midnight
        var sale1Utc = LocalToUtc(2026, 4, 1, 10, 0, MexicoCityTz);  // 16:00 UTC same day
        var sale2Utc = LocalToUtc(2026, 4, 1, 20, 0, MexicoCityTz);  // 02:00 UTC next day
        var sale3Utc = LocalToUtc(2026, 4, 1, 23, 45, MexicoCityTz); // 05:45 UTC next day

        await SeedSaleAsync(sale1Utc, id: 1);
        await SeedSaleAsync(sale2Utc, id: 2);
        await SeedSaleAsync(sale3Utc, id: 3);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(3),
            "All three sales made on 01/Apr local must be included regardless of UTC day.");
    }

    [Test]
    public async Task DailyPreview_SalesBelongingToDifferentLocalDays_CorrectlySplit()
    {
        // 01/Apr: one sale at 15:00 local + one at 23:00 local (both UTC 01/Apr and 02/Apr)
        // 02/Apr: one sale at 09:00 local
        // Filter for 01/Apr only → expect 2
        var saleA = LocalToUtc(2026, 4, 1, 15, 0, MexicoCityTz);  // 21:00 UTC 01/Apr
        var saleB = LocalToUtc(2026, 4, 1, 23, 0, MexicoCityTz);  // 05:00 UTC 02/Apr
        var saleC = LocalToUtc(2026, 4, 2,  9, 0, MexicoCityTz);  // 15:00 UTC 02/Apr

        await SeedSaleAsync(saleA, id: 1);
        await SeedSaleAsync(saleB, id: 2);
        await SeedSaleAsync(saleC, id: 3);

        var result = await _service.PreviewAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.SaleCount, Is.EqualTo(2),
            "Only the two sales made locally on 01/Apr must be included.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a local date/time in the given timezone to UTC DateTime with Kind=Utc.
    /// Mirrors what the app does when storing SaleDate.
    /// </summary>
    private static DateTime LocalToUtc(int year, int month, int day, int hour, int minute,
        TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, 0),
            DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    private async Task SeedSaleAsync(DateTime saleDateUtc, long id = 1)
    {
        await using var context = new ApplicationDbContext(_dbOptions);

        // Minimal customer required by FK
        if (!context.Customers.Any(c => c.Id == 1))
        {
            context.Customers.Add(new App.Models.Shared.Customer
            {
                Id = 1,
                Name = "Público General",
                CountryCode = "MX",
                CreatedBy = "seed",
                ModifiedBy = "seed"
            });
        }

        context.Sales.Add(new Sale
        {
            Id = id,
            CustomerId = 1,
            SaleDate = DateTime.SpecifyKind(saleDateUtc, DateTimeKind.Utc),
            SaleType = SaleType.Public,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            Subtotal = 100m,
            TaxAmount = 16m,
            Total = 116m,
            CreatedBy = "seed",
            ModifiedBy = "seed"
        });

        await context.SaveChangesAsync();
    }

    private GlobalInvoiceService BuildService()
    {
        var factory = new TestDbContextFactory(_dbOptions);
        var localizer = new Mock<IStringLocalizer<GlobalInvoiceService>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto { CountryCode = "MX" });

        var taxRateMock = new Mock<ITaxRateService>();
        taxRateMock.Setup(r => r.GetEffectiveRateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(0.16m);

        return new GlobalInvoiceService(
            contextFactory: factory,
            xmlService: new Mock<IMexicoCfdiXmlService>().Object,
            signingService: new Mock<IMexicoCsdSigningService>().Object,
            pacService: new Mock<ISwSapienService>().Object,
            pacSettingsService: new Mock<IMexicoPacSettingsService>().Object,
            taxSettingsService: taxSettingsMock.Object,
            taxRateService: taxRateMock.Object,
            companySettingsService: _companySvcMock.Object,
            pdfService: new Mock<IPdfService>().Object,
            emailTemplateService: new Mock<IEmailTemplateService>().Object,
            fiscalCatalogService: new Mock<IMexicoFiscalCatalogService>().Object,
            currentUserService: new Mock<ICurrentUserService>().Object,
            dateTime: new Mock<App.Shared.Services.IDateTime>().Object,
            applicationOptions: Microsoft.Extensions.Options.Options.Create(new App.Core.Options.ApplicationOptions { Name = "Test", BaseUrl = "http://localhost" }),
            localizer: localizer.Object,
            logger: NullLogger<GlobalInvoiceService>.Instance);
    }

    // Minimal IDbContextFactory implementation for tests
    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : Microsoft.EntityFrameworkCore.IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}
