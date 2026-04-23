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
using App.Models.Shop;
using App.Services.Reports;
using App.Shared.Services;

using System.Globalization;

namespace App.Services.Tests.Reports;

/// <summary>
/// Guards against ArgumentException ("key already added") when a sale has more than one
/// MexicoInvoice row (e.g. one Stamped + one Cancelled after re-stamp).
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
