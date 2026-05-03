using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

using App.Core.Constants;
using App.Core.DTOs.Reports;
using App.Core.DTOs.Shop;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Reports;
using App.Shared.Services;

using System.Globalization;

namespace App.Services.Tests.Reports;

/// <summary>
/// Integration tests for SalesReportService export behavior.
///
/// NOTE on EF Core InMemory + Include: EF Core 7+ InMemory treats Include() on a required
/// navigation as an INNER JOIN. Tests must seed related entities (e.g. Customer) for the sale
/// to be materialized in ToListAsync results.
/// </summary>
[TestFixture]
public class SalesHistoryExportTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private SalesReportService _service = null!;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .Options;

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns((Sale s) => new SaleDto
            {
                Id = s.Id,
                CustomerId = s.CustomerId,
                CustomerName = "Test Customer",
                SaleDate = s.SaleDate,
                Subtotal = s.Total,
                TaxAmount = 0m,
                DiscountAmount = 0m,
                Total = s.Total,
                TaxRate = 0.16m,
                Status = s.Status,
                SaleType = s.SaleType,
                Payments = []
            });

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetCurrentTimeZoneAsync())
            .ReturnsAsync(TimeZoneInfo.Utc);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.ToUtc(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
            .Returns((DateTime dt, TimeZoneInfo _) => dt);
        dateTimeMock.Setup(d => d.FormatToTimezone(It.IsAny<DateTime>(), It.IsAny<TimeZoneInfo>()))
            .Returns((DateTime dt, TimeZoneInfo _) => dt.ToString("yyyy-MM-dd HH:mm"));

        var localizerMock = new Mock<IStringLocalizer<SalesReportService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var pdfServiceMock = new Mock<IPdfService>();

        var exportOptions = Options.Create(new ExportOptions { MaxExportRecords = 10000 });

        _service = new SalesReportService(
            new TestDbContextFactory(_dbOptions),
            mapperMock.Object,
            NullLogger<SalesReportService>.Instance,
            localizerMock.Object,
            pdfServiceMock.Object,
            companySettingsMock.Object,
            dateTimeMock.Object,
            exportOptions);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Original regression: ArgumentException when a sale has multiple invoices
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Guards against ArgumentException ("key already added") when a sale has more than one
    /// MexicoInvoice row (e.g. one Stamped + one Cancelled after re-stamp).
    /// </summary>
    [Test]
    public async Task ExportSalesHistoryToExcel_SaleWithMultipleInvoices_DoesNotThrow()
    {
        // Arrange — sale 109 has a cancelled invoice + a stamped one
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Sales.Add(new Sale
        {
            Id = 109,
            CustomerId = 1,
            SaleDate = DateTime.UtcNow,
            Total = 100m,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            SaleType = SaleType.Public,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        });
        ctx.Set<MexicoInvoice>().AddRange(
            MakeInvoice(1, 109, "Cancelled", DateTime.UtcNow.AddDays(-1)),
            MakeInvoice(2, 109, "Stamped",   DateTime.UtcNow)
        );
        await ctx.SaveChangesAsync();

        var request = new SalesReportRequestDto { PageSize = 100 };

        // Act + Assert — must not throw ArgumentException
        Assert.DoesNotThrowAsync(async () =>
        {
            var (bytes, fileName) = await _service.ExportSalesHistoryToExcelAsync(
                request, CultureInfo.InvariantCulture, CancellationToken.None);

            Assert.That(bytes, Is.Not.Empty);
            Assert.That(fileName, Does.EndWith(".xlsx"));
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Date filtering — UTC boundary regression (sale at 23:59 local time)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EF InMemory sanity-check: the date filter in GetBaseQueryAsync (endDate + 1 day)
    /// must correctly include a sale at 23:59 local (next day in UTC).
    /// Uses CountAsync to avoid the InMemory inner-join behavior on required navigations.
    /// </summary>
    [Test]
    public async Task DateFilter_SaleAt2359Local_InMemoryQueryIncludesSale()
    {
        // endDate as sent by frontend: "Apr 30 00:00 UTC-6" → "Apr 30 06:00 UTC"
        var endDateUtc = new DateTime(2026, 4, 30, 6, 0, 0, DateTimeKind.Utc);
        var nextDay    = endDateUtc.AddDays(1);   // May 1 06:00 UTC

        // Sale at 23:59 local (UTC-6) = May 1 05:59 UTC
        var saleDate = new DateTime(2026, 5, 1, 5, 59, 0, DateTimeKind.Utc);

        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Sales.Add(new Sale
        {
            Id = 999,
            CustomerId = 1,
            SaleDate = saleDate,
            Total = 10m,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            SaleType = SaleType.Public,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        // Query directly — same logic as the fixed GetBaseQueryAsync
        await using var readCtx = new ApplicationDbContext(_dbOptions);
        var count = await readCtx.Sales
            .Where(s => s.SaleDate < nextDay)
            .CountAsync();

        Assert.That(count, Is.EqualTo(1),
            $"EF InMemory should find the sale at {saleDate:O} with nextDay={nextDay:O}");
    }

    /// <summary>
    /// Sale at 23:59 local (UTC-6) = 05:59 UTC next day must appear in the Excel export
    /// when endDate is "April 30 00:00 local" sent as UTC = April 30 06:00 UTC.
    ///
    /// Before the fix, GetBaseQueryAsync applied .Date+1day-1tick on the already-UTC endDate,
    /// producing Apr-30 23:59:59 UTC as the cutoff — excluding the sale.
    /// After the fix, it uses endDate.AddDays(1) = May 1 06:00 UTC, which includes the sale.
    /// </summary>
    [Test]
    public async Task ExportSalesHistoryToExcel_SaleAt2359Local_IncludedInSameDayExport()
    {
        // Frontend converts "30/04/2026 00:00 Mexico City (UTC-6)" → "30/04/2026 06:00 UTC"
        var endDateUtc = new DateTime(2026, 4, 30, 6, 0, 0, DateTimeKind.Utc);

        // Sale at 30/04/2026 23:59 local (UTC-6) = 01/05/2026 05:59 UTC
        var saleDate = new DateTime(2026, 5, 1, 5, 59, 0, DateTimeKind.Utc);

        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Set<Customer>().Add(MakeCustomer(1));
        ctx.Sales.Add(new Sale
        {
            Id = 250,
            CustomerId = 1,
            SaleDate = saleDate,
            Total = 100m,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            SaleType = SaleType.Public,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var request = new SalesReportRequestDto { EndDate = endDateUtc, PageSize = 100 };

        var (bytes, _) = await _service.ExportSalesHistoryToExcelAsync(
            request, CultureInfo.InvariantCulture, CancellationToken.None);

        Assert.That(bytes, Is.Not.Empty);

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        using var package = new OfficeOpenXml.ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets[0];
        int dataRows = (ws.Dimension?.Rows ?? 1) - 1;

        Assert.That(dataRows, Is.GreaterThan(0),
            $"Excel must have data rows — sale at 23:59 local (05:59 UTC) must be included");

        var saleIds = Enumerable.Range(2, dataRows)
            .Select(r => ws.Cells[r, 1].GetValue<long>())
            .ToList();

        Assert.That(saleIds, Contains.Item(250L),
            "Sale at 23:59 local (05:59 UTC next day) must be included when endDate covers that calendar day");
    }

    /// <summary>
    /// A sale one hour after the local UTC boundary of the end day must NOT appear in the export.
    /// </summary>
    [Test]
    public async Task ExportSalesHistoryToExcel_SaleAfterEndDateBoundary_Excluded()
    {
        // endDate = Apr 30 06:00 UTC (= Apr 30 00:00 Mexico City UTC-6)
        var endDateUtc = new DateTime(2026, 4, 30, 6, 0, 0, DateTimeKind.Utc);

        // Sale on May 1 at 07:00 UTC (= May 1 01:00 local) — the next day
        var saleDate = new DateTime(2026, 5, 1, 7, 0, 0, DateTimeKind.Utc);

        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Set<Customer>().Add(MakeCustomer(1));
        ctx.Sales.Add(new Sale
        {
            Id = 300,
            CustomerId = 1,
            SaleDate = saleDate,
            Total = 50m,
            Status = App.Core.Enums.Shop.SaleStatus.Created,
            SaleType = SaleType.Public,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var request = new SalesReportRequestDto { EndDate = endDateUtc, PageSize = 100 };

        var (bytes, _) = await _service.ExportSalesHistoryToExcelAsync(
            request, CultureInfo.InvariantCulture, CancellationToken.None);

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        using var package = new OfficeOpenXml.ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets[0];
        var saleIds = Enumerable.Range(2, (ws.Dimension?.Rows ?? 1) - 1)
            .Select(r => ws.Cells[r, 1].GetValue<long>())
            .ToList();

        Assert.That(saleIds, Does.Not.Contain(300L),
            "Sale dated the day after endDate must not appear in the export");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static Customer MakeCustomer(long id) => new()
    {
        Id = id,
        Name = "Test Customer",
        CountryCode = "MX",
        CreatedBy = "test",
        CreatedAt = DateTime.UtcNow
    };

    private static MexicoInvoice MakeInvoice(long id, long saleId, string status, DateTime createdAt) => new()
    {
        Id = id, SaleId = saleId, Status = status, CreatedAt = createdAt, CreatedBy = "test",
        CfdiUse = "G03", PaymentForm = "01", PaymentMethod = "PUE",
        CustomerRfc = "XAXX010101000", CustomerLegalName = "PUBLICO EN GENERAL",
        CustomerPostalCode = "06600", CustomerFiscalRegime = "616",
        IssuerRfc = "TEST010101AAA", IssuerLegalName = "TEST SA DE CV",
        IssuerFiscalRegime = "601", IssuerPostalCode = "06600"
    };

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}
