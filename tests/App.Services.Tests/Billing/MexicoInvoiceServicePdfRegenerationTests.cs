using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Options;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Models.Shop;
using App.Services.Billing;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using SaleStatus = App.Core.Enums.Shop.SaleStatus;
using SaleType = App.Core.Constants.SaleType;

namespace App.Services.Tests.Billing;

/// <summary>
/// Covers the bug fixed alongside ADR "PDF regeneration and CFDI display dates":
///
///   1. The PDF's "FECHA DE EMISIÓN" must match the CFDI's Fecha node (RequestedInvoiceDate when
///      the invoice was backdated), not the PAC stamp timestamp.
///   2. Both dates must be converted from UTC storage to the issuer's local timezone before display.
///   3. Overwriting the PDF of an invoice that already has one is gated by
///      MexicoPacSettings.AllowPdfRegenerationForStampedInvoices — disabled by default.
///
/// These are integration tests (EF Core InMemory) because RegeneratePdfAsync's behavior depends
/// on reading/writing MexicoInvoiceFiles rows, which mocking the DbContext would not verify.
/// </summary>
[TestFixture]
public class MexicoInvoiceServicePdfRegenerationTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private static readonly TimeZoneInfo MexicoCityTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<IMexicoPacSettingsService> _pacSettingsServiceMock = null!;
    private Mock<IEmailTemplateService> _emailTemplateServiceMock = null!;
    private Dictionary<string, object>? _capturedPdfData;
    private MexicoInvoiceService _service = null!;

    private const long InvoiceId = 100;
    private const long SaleId = 1;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .Options;

        _capturedPdfData = null;

        _pacSettingsServiceMock = new Mock<IMexicoPacSettingsService>();
        _pacSettingsServiceMock.Setup(p => p.GetAsync()).ReturnsAsync(new MexicoPacSettingsDto
        {
            AllowPdfRegenerationForStampedInvoices = false
        });

        var taxSettingsMock = new Mock<ITaxSettingsService>();
        taxSettingsMock.Setup(t => t.GetSettingsAsync()).ReturnsAsync(new TaxSettingsDto
        {
            CountryCode = "MX",
            PostalCodeIanaTimeZoneId = "America/Mexico_City"
        });

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetCurrentTimeZoneAsync()).ReturnsAsync(MexicoCityTz);

        _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
        _emailTemplateServiceMock.Setup(e => e.GetStaticFileBase64Async(It.IsAny<string>()))
            .ReturnsAsync("base64logo");
        _emailTemplateServiceMock
            .Setup(e => e.GetTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((_, data, _) => _capturedPdfData = (Dictionary<string, object>)data)
            .ReturnsAsync("<html>invoice</html>");

        var pdfServiceMock = new Mock<IPdfService>();
        pdfServiceMock.Setup(p => p.GeneratePdfFromHtmlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync("test-user");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var localizerMock = new Mock<IStringLocalizer<MexicoInvoiceService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var appOptions = Options.Create(new ApplicationOptions { Name = "TestApp", BaseUrl = "https://test.com" });

        _service = new MexicoInvoiceService(
            new TestDbContextFactory(_dbOptions),
            new Mock<IMexicoCfdiXmlService>().Object,
            new Mock<IMexicoCsdSigningService>().Object,
            new Mock<ISwSapienService>().Object,
            _pacSettingsServiceMock.Object,
            taxSettingsMock.Object,
            new Mock<IMexicoStampAlertService>().Object,
            pdfServiceMock.Object,
            new Mock<IEmailService>().Object,
            _emailTemplateServiceMock.Object,
            localizerMock.Object,
            appOptions,
            dateTimeMock.Object,
            companySettingsMock.Object,
            currentUserServiceMock.Object,
            NullLogger<MexicoInvoiceService>.Instance);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var context = new ApplicationDbContext(_dbOptions);

        context.UnitMeasures.Add(new UnitMeasure
        {
            Id = 1, Code = "PZA", Name = "Pieza", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.Customers.Add(new Customer
        {
            Id = 1, Name = "Test Customer", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.Products.Add(new Product
        {
            Id = 1, Code = "P0001", Name = "Test Product", Brand = "Test",
            Price = 100m, Cost = 50m, IsTaxable = true, IsActive = true,
            UnitMeasureId = 1, Content = 1, QuantityStep = 1,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });
        context.Sales.Add(new Sale
        {
            Id = SaleId, CustomerId = 1, LocationId = 1,
            SaleType = SaleType.Public, Status = SaleStatus.Created,
            Subtotal = 100m, TaxAmount = 16m, Total = 116m,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow, ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
        });
        context.SaleDetails.Add(new SaleDetail
        {
            Id = 1, SaleId = SaleId, ProductId = 1,
            Quantity = 1, UnitPrice = 100m, Subtotal = 100m,
            TaxRate = 0.16m, TaxAmount = 16m, Total = 116m,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
    }

    /// <summary>Seeds a stamped invoice with a backdated Fecha (RequestedInvoiceDate) distinct from the PAC stamp time.</summary>
    private static MexicoInvoice BuildStampedInvoice(DateTime? requestedInvoiceDateUtc, DateTime stampDateUtc) => new()
    {
        Id = InvoiceId,
        SaleId = SaleId,
        Serie = "A",
        Folio = 183,
        Status = "Stamped",
        IsStamped = true,
        Uuid = "fcb004c3-079d-4c1a-bb68-9d3735bf95d9",
        RequestedInvoiceDate = requestedInvoiceDateUtc,
        StampDate = stampDateUtc,
        CfdiUse = "S01",
        PaymentForm = "03",
        PaymentMethod = "PUE",
        CustomerRfc = "XAXX010101000",
        CustomerLegalName = "Público General",
        CustomerPostalCode = "36670",
        CustomerFiscalRegime = "616",
        IssuerRfc = "GOFA910316UC8",
        IssuerLegalName = "Ana Maria Gonzalez Frias",
        IssuerFiscalRegime = "612",
        IssuerPostalCode = "36670",
        Subtotal = 663.79m,
        TaxAmount = 106.21m,
        Total = 770.00m,
        CreatedBy = "seed",
        CreatedAt = DateTime.UtcNow,
        ModifiedBy = "seed",
        ModifiedAt = DateTime.UtcNow
    };

    // ──────────────────────────────────────────────────────────────────────
    // Bug fix: issue_date / stamp_date must be correct and timezone-converted
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RegeneratePdfAsync_WithBackdatedInvoice_IssueDateMatchesRequestedInvoiceDate_NotStampDate()
    {
        // Mirrors invoice A183: antedatada al 30/06/2026 13:30 local, timbrada 01/07/2026 15:57 local.
        var requestedLocal = new DateTime(2026, 6, 30, 13, 30, 0);
        var stampLocal = new DateTime(2026, 7, 1, 15, 57, 0);
        var requestedUtc = TimeZoneInfo.ConvertTimeToUtc(requestedLocal, MexicoCityTz);
        var stampUtc = TimeZoneInfo.ConvertTimeToUtc(stampLocal, MexicoCityTz);

        await SeedInvoiceAsync(BuildStampedInvoice(requestedUtc, stampUtc));
        _pacSettingsServiceMock.Setup(p => p.GetAsync())
            .ReturnsAsync(new MexicoPacSettingsDto { AllowPdfRegenerationForStampedInvoices = true });
        await SeedExistingPdfAsync();

        var result = await _service.RegeneratePdfAsync(InvoiceId);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedPdfData, Is.Not.Null);
        Assert.That(_capturedPdfData!["issue_date"], Is.EqualTo("30/06/2026 13:30"),
            "FECHA DE EMISIÓN must reflect the CFDI's Fecha (RequestedInvoiceDate), not the PAC stamp time");
        Assert.That(_capturedPdfData!["stamp_date"], Is.EqualTo("01/07/2026 15:57:00"),
            "FECHA DE CERTIFICACIÓN must be converted to the issuer's local timezone");
    }

    [Test]
    public async Task RegeneratePdfAsync_WithoutBackdating_IssueDateFallsBackToStampDate()
    {
        var stampLocal = new DateTime(2026, 7, 2, 17, 28, 0);
        var stampUtc = TimeZoneInfo.ConvertTimeToUtc(stampLocal, MexicoCityTz);

        await SeedInvoiceAsync(BuildStampedInvoice(requestedInvoiceDateUtc: null, stampUtc));
        _pacSettingsServiceMock.Setup(p => p.GetAsync())
            .ReturnsAsync(new MexicoPacSettingsDto { AllowPdfRegenerationForStampedInvoices = true });
        await SeedExistingPdfAsync();

        var result = await _service.RegeneratePdfAsync(InvoiceId);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedPdfData!["issue_date"], Is.EqualTo("02/07/2026 17:28"));
        Assert.That(_capturedPdfData!["stamp_date"], Is.EqualTo("02/07/2026 17:28:00"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Feature flag gating
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RegeneratePdfAsync_ExistingPdf_SettingDisabled_ReturnsFailure()
    {
        await SeedInvoiceAsync(BuildStampedInvoice(null, DateTime.UtcNow));
        await SeedExistingPdfAsync();
        // Default mock setup already returns AllowPdfRegenerationForStampedInvoices = false

        var result = await _service.RegeneratePdfAsync(InvoiceId);

        Assert.That(result.IsSuccess, Is.False);

        using var context = new ApplicationDbContext(_dbOptions);
        var pdfCount = await context.MexicoInvoiceFiles
            .CountAsync(f => f.InvoiceId == InvoiceId && f.FileType == "PDF" && f.IsDeleted == 0);
        Assert.That(pdfCount, Is.EqualTo(1), "The existing PDF must not be touched when the setting is disabled");
    }

    [Test]
    public async Task RegeneratePdfAsync_ExistingPdf_SettingEnabled_Succeeds()
    {
        await SeedInvoiceAsync(BuildStampedInvoice(null, DateTime.UtcNow));
        await SeedExistingPdfAsync();
        _pacSettingsServiceMock.Setup(p => p.GetAsync())
            .ReturnsAsync(new MexicoPacSettingsDto { AllowPdfRegenerationForStampedInvoices = true });

        var result = await _service.RegeneratePdfAsync(InvoiceId);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task RegeneratePdfAsync_NoExistingPdf_SucceedsRegardlessOfSetting()
    {
        // First-time PDF generation (e.g. stamping succeeded but PDF creation failed) is always allowed.
        await SeedInvoiceAsync(BuildStampedInvoice(null, DateTime.UtcNow));
        // No PDF seeded, and setting stays disabled (default mock)

        var result = await _service.RegeneratePdfAsync(InvoiceId);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private async Task SeedInvoiceAsync(MexicoInvoice invoice)
    {
        using var context = new ApplicationDbContext(_dbOptions);
        context.MexicoInvoices.Add(invoice);
        await context.SaveChangesAsync();
    }

    private async Task SeedExistingPdfAsync()
    {
        using var context = new ApplicationDbContext(_dbOptions);
        context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
        {
            InvoiceId = InvoiceId,
            FileType = "PDF",
            FileData = new byte[] { 0x25, 0x50, 0x44, 0x46 },
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplicationDbContext(options));
    }
}
