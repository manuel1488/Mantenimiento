using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;
using App.Core.DTOs.Settings;
using App.Core.Constants;
using SaleStatus = App.Core.Enums.Shop.SaleStatus;
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

namespace App.Services.Tests.Billing;

/// <summary>
/// Tests that MexicoInvoiceService uses ICurrentUserService for all audit fields
/// (CreatedBy, ModifiedBy, DeletedBy) instead of hardcoded "System".
/// </summary>
[TestFixture]
public class MexicoInvoiceServiceAuditTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private MexicoInvoiceService _service = null!;
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    private Mock<IMexicoCfdiXmlService> _xmlServiceMock = null!;
    private Mock<IMexicoCsdSigningService> _signingServiceMock = null!;
    private Mock<ISwSapienService> _pacServiceMock = null!;
    private Mock<IMexicoPacSettingsService> _pacSettingsServiceMock = null!;
    private Mock<ITaxSettingsService> _taxSettingsServiceMock = null!;
    private Mock<IMexicoStampAlertService> _stampAlertServiceMock = null!;
    private Mock<IPdfService> _pdfServiceMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IEmailTemplateService> _emailTemplateServiceMock = null!;
    private Mock<ICompanySettingsService> _companySettingsServiceMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IDateTime> _dateTimeMock = null!;

    private const string TestUserId = "user-abc-123";
    private const long SaleId = 1;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .Options;

        // Current user — the value we expect in all audit fields
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync(TestUserId);

        // DateTime
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(d => d.Now).Returns(new DateTime(2026, 3, 24, 18, 0, 0, DateTimeKind.Utc));

        // PAC settings — configured with CSD
        _pacSettingsServiceMock = new Mock<IMexicoPacSettingsService>();
        _pacSettingsServiceMock.Setup(p => p.GetAsync()).ReturnsAsync(new MexicoPacSettingsDto
        {
            ProviderName = "SwSapien",
            User = "test-user",
            HasPassword = true,
            HasCsdCertificate = true,
            HasCsdPrivateKey = true,
            InvoiceSerie = "A",
            StartFolio = 1,
            FolioLength = 0
        });
        _pacSettingsServiceMock.Setup(p => p.GetCsdCertificateBytesAsync())
            .ReturnsAsync(Result<byte[]>.Success(new byte[] { 1, 2, 3 }));
        _pacSettingsServiceMock.Setup(p => p.GetCsdPrivateKeyBytesAsync())
            .ReturnsAsync(Result<byte[]>.Success(new byte[] { 4, 5, 6 }));
        _pacSettingsServiceMock.Setup(p => p.GetCsdPasswordAsync())
            .ReturnsAsync(Result<string>.Success("password"));

        // Tax settings — issuer data
        _taxSettingsServiceMock = new Mock<ITaxSettingsService>();
        _taxSettingsServiceMock.Setup(t => t.GetSettingsAsync()).ReturnsAsync(new TaxSettingsDto
        {
            Id = 1,
            CountryCode = "MX",
            BusinessName = "Test Business",
            TaxId = "TEST000000AA0",
            FiscalRegime = "601",
            PostalCode = "36614",
            PostalCodeIanaTimeZoneId = "America/Mexico_City"
        });

        // XML generation
        _xmlServiceMock = new Mock<IMexicoCfdiXmlService>();
        _xmlServiceMock.Setup(x => x.GenerateXmlAsync(It.IsAny<App.Core.Models.Cfdi.V40.Comprobante>()))
            .ReturnsAsync(Result<string>.Success("<cfdi:Comprobante/>"));

        // Signing
        _signingServiceMock = new Mock<IMexicoCsdSigningService>();
        _signingServiceMock.Setup(s => s.SignXmlAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(Result<string>.Success("<cfdi:Comprobante signed/>"));

        // PAC stamp — successful
        _pacServiceMock = new Mock<ISwSapienService>();
        _pacServiceMock.Setup(p => p.StampAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<SwSapienStampData>.Success(new SwSapienStampData
            {
                Uuid = "12345678-1234-1234-1234-123456789012",
                Cfdi = "<cfdi:Comprobante timbrado/>",
                NoCertificadoSat = "00001000000500003416",
                NoCertificadoCfdi = "00001000000723047907",
                SelloSat = "sello-sat-test",
                SelloCfdi = "sello-cfdi-test",
                CadenaOriginalSat = "||cadena||"
            }));

        // Stamp alert
        _stampAlertServiceMock = new Mock<IMexicoStampAlertService>();

        // PDF
        _pdfServiceMock = new Mock<IPdfService>();
        _pdfServiceMock.Setup(p => p.GeneratePdfFromHtmlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF

        // Email template
        _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
        _emailTemplateServiceMock.Setup(e => e.GetStaticFileBase64Async(It.IsAny<string>()))
            .ReturnsAsync("base64logo");
        _emailTemplateServiceMock.Setup(e => e.GetTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>invoice</html>");

        // Email
        _emailServiceMock = new Mock<IEmailService>();

        // Company settings
        _companySettingsServiceMock = new Mock<ICompanySettingsService>();
        _companySettingsServiceMock.Setup(c => c.GetCurrentTimeZoneAsync())
            .ReturnsAsync(TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"));

        // Localizer
        var localizerMock = new Mock<IStringLocalizer<MexicoInvoiceService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        // Application options
        var appOptions = Options.Create(new ApplicationOptions
        {
            Name = "TestApp",
            BaseUrl = "https://test.com"
        });

        // Build service
        var contextFactory = new TestDbContextFactory(_dbOptions);
        _service = new MexicoInvoiceService(
            contextFactory,
            _xmlServiceMock.Object,
            _signingServiceMock.Object,
            _pacServiceMock.Object,
            _pacSettingsServiceMock.Object,
            _taxSettingsServiceMock.Object,
            _stampAlertServiceMock.Object,
            _pdfServiceMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            localizerMock.Object,
            appOptions,
            _dateTimeMock.Object,
            _companySettingsServiceMock.Object,
            _currentUserServiceMock.Object,
            NullLogger<MexicoInvoiceService>.Instance);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var context = new ApplicationDbContext(_dbOptions);

        var unitMeasure = new UnitMeasure
        {
            Id = 1, Code = "LTR", Name = "Litros", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        };
        context.UnitMeasures.Add(unitMeasure);

        var customer = new Customer
        {
            Id = 1, Name = "Test Customer", CountryCode = "MX",
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        var product = new Product
        {
            Id = 1, Code = "P0001", Name = "Test Product", Brand = "Test",
            Price = 100m, Cost = 50m, IsTaxable = true, IsActive = true,
            UnitMeasureId = 1, Content = 1, QuantityStep = 1,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);

        var sale = new Sale
        {
            Id = SaleId, CustomerId = 1, LocationId = 1,
            SaleType = SaleType.Public, Status = SaleStatus.Created,
            Subtotal = 100m, TaxAmount = 16m, DiscountAmount = 0m, Total = 116m,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed", ModifiedAt = DateTime.UtcNow
        };
        context.Sales.Add(sale);

        var detail = new SaleDetail
        {
            Id = 1, SaleId = SaleId, ProductId = 1,
            Quantity = 1, UnitPrice = 100m, Subtotal = 100m,
            TaxRate = 0.16m, TaxAmount = 16m,
            DiscountAmount = 0m, DiscountPercentage = 0m, Total = 116m,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        };
        context.SaleDetails.Add(detail);

        context.SaveChanges();
    }

    [Test]
    public async Task CreateAndStampAsync_SetsCurrentUserOnInvoiceAuditFields()
    {
        // Arrange
        var dto = new CreateMexicoInvoiceDto
        {
            SaleId = SaleId,
            CfdiUse = "G03",
            PaymentForm = "01",
            PaymentMethod = "PUE",
            CustomerRfc = "XAXX010101000",
            CustomerLegalName = "Test Customer",
            CustomerPostalCode = "36614",
            CustomerFiscalRegime = "612"
        };

        // Act
        var result = await _service.CreateAndStampAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Stamp failed: {result.Error}");

        using var context = new ApplicationDbContext(_dbOptions);
        var invoice = await context.MexicoInvoices.FirstAsync();

        Assert.That(invoice.CreatedBy, Is.EqualTo(TestUserId),
            "Invoice.CreatedBy should be the current user, not 'System'");
        Assert.That(invoice.ModifiedBy, Is.EqualTo(TestUserId),
            "Invoice.ModifiedBy should be the current user, not 'System'");
    }

    [Test]
    public async Task CreateAndStampAsync_SetsCurrentUserOnFileAuditFields()
    {
        // Arrange
        var dto = new CreateMexicoInvoiceDto
        {
            SaleId = SaleId,
            CfdiUse = "G03",
            PaymentForm = "01",
            PaymentMethod = "PUE",
            CustomerRfc = "XAXX010101000",
            CustomerLegalName = "Test Customer",
            CustomerPostalCode = "36614",
            CustomerFiscalRegime = "612"
        };

        // Act
        var result = await _service.CreateAndStampAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Stamp failed: {result.Error}");

        using var context = new ApplicationDbContext(_dbOptions);
        var files = await context.MexicoInvoiceFiles.ToListAsync();

        Assert.That(files, Has.Count.GreaterThan(0), "Should have at least one file (XML)");
        foreach (var file in files)
        {
            Assert.That(file.CreatedBy, Is.EqualTo(TestUserId),
                $"File({file.FileType}).CreatedBy should be the current user, not 'System'");
            Assert.That(file.ModifiedBy, Is.EqualTo(TestUserId),
                $"File({file.FileType}).ModifiedBy should be the current user, not 'System'");
        }
    }

    [Test]
    public async Task RetryStampAsync_SetsCurrentUserOnUpdatedInvoice()
    {
        // Arrange — create a failed invoice first
        using (var context = new ApplicationDbContext(_dbOptions))
        {
            var invoice = new MexicoInvoice
            {
                Id = 100,
                SaleId = SaleId,
                Serie = "A",
                Folio = 1,
                Status = "StampError",
                StampError = "Previous error",
                IsStamped = false,
                CfdiUse = "G03",
                PaymentForm = "01",
                PaymentMethod = "PUE",
                CustomerRfc = "XAXX010101000",
                CustomerLegalName = "Test Customer",
                CustomerPostalCode = "36614",
                CustomerFiscalRegime = "612",
                IssuerRfc = "TEST000000AA0",
                IssuerLegalName = "Test Business",
                IssuerFiscalRegime = "601",
                IssuerPostalCode = "36614",
                Subtotal = 100m,
                TaxAmount = 16m,
                Total = 116m,
                CreatedBy = "old-user",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ModifiedBy = "old-user",
                ModifiedAt = DateTime.UtcNow.AddDays(-1)
            };
            context.MexicoInvoices.Add(invoice);

            // Old error XML file
            context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
            {
                Id = 200,
                InvoiceId = 100,
                FileType = "XML",
                FileData = System.Text.Encoding.UTF8.GetBytes("<old-error-xml/>"),
                CreatedBy = "old-user",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _service.RetryStampAsync(100);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Retry failed: {result.Error}");

        using (var context = new ApplicationDbContext(_dbOptions))
        {
            var invoice = await context.MexicoInvoices.FindAsync(100L);
            Assert.That(invoice, Is.Not.Null);
            Assert.That(invoice!.Status, Is.EqualTo("Stamped"));
            Assert.That(invoice.ModifiedBy, Is.EqualTo(TestUserId),
                "After retry, invoice.ModifiedBy should be the current user");
        }
    }

    [Test]
    public async Task RetryStampAsync_SetsCurrentUserOnNewFiles()
    {
        // Arrange — create a failed invoice with an error XML file
        using (var context = new ApplicationDbContext(_dbOptions))
        {
            context.MexicoInvoices.Add(new MexicoInvoice
            {
                Id = 101,
                SaleId = SaleId,
                Serie = "A",
                Folio = 2,
                Status = "StampError",
                StampError = "PAC error",
                IsStamped = false,
                CfdiUse = "G03",
                PaymentForm = "01",
                PaymentMethod = "PUE",
                CustomerRfc = "XAXX010101000",
                CustomerLegalName = "Test Customer",
                CustomerPostalCode = "36614",
                CustomerFiscalRegime = "612",
                IssuerRfc = "TEST000000AA0",
                IssuerLegalName = "Test Business",
                IssuerFiscalRegime = "601",
                IssuerPostalCode = "36614",
                Subtotal = 100m,
                TaxAmount = 16m,
                Total = 116m,
                CreatedBy = "old-user",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ModifiedBy = "old-user",
                ModifiedAt = DateTime.UtcNow.AddDays(-1)
            });
            context.MexicoInvoiceFiles.Add(new MexicoInvoiceFile
            {
                Id = 201,
                InvoiceId = 101,
                FileType = "XML",
                FileData = System.Text.Encoding.UTF8.GetBytes("<old/>"),
                CreatedBy = "old-user",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _service.RetryStampAsync(101);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Retry failed: {result.Error}");

        using (var context = new ApplicationDbContext(_dbOptions))
        {
            // New files created during retry should have current user
            var newFiles = await context.MexicoInvoiceFiles
                .Where(f => f.InvoiceId == 101 && f.IsDeleted == 0)
                .ToListAsync();

            Assert.That(newFiles, Has.Count.GreaterThan(0), "Should have new files after retry");
            foreach (var file in newFiles)
            {
                Assert.That(file.CreatedBy, Is.EqualTo(TestUserId),
                    $"New file({file.FileType}).CreatedBy should be the current user");
                Assert.That(file.ModifiedBy, Is.EqualTo(TestUserId),
                    $"New file({file.FileType}).ModifiedBy should be the current user");
            }
        }
    }

    [Test]
    public async Task CreateAndStampAsync_NeverUsesHardcodedSystemString()
    {
        // Arrange
        var dto = new CreateMexicoInvoiceDto
        {
            SaleId = SaleId,
            CfdiUse = "G03",
            PaymentForm = "01",
            PaymentMethod = "PUE",
            CustomerRfc = "XAXX010101000",
            CustomerLegalName = "Test Customer",
            CustomerPostalCode = "36614",
            CustomerFiscalRegime = "612"
        };

        // Act
        var result = await _service.CreateAndStampAsync(dto);

        // Assert
        Assert.That(result.IsSuccess, Is.True, $"Stamp failed: {result.Error}");

        using var context = new ApplicationDbContext(_dbOptions);
        var invoice = await context.MexicoInvoices.FirstAsync();
        var files = await context.MexicoInvoiceFiles.ToListAsync();

        // No entity should have "System" as audit user
        Assert.That(invoice.CreatedBy, Is.Not.EqualTo("System"),
            "Invoice.CreatedBy must not be hardcoded 'System'");
        Assert.That(invoice.ModifiedBy, Is.Not.EqualTo("System"),
            "Invoice.ModifiedBy must not be hardcoded 'System'");

        foreach (var file in files)
        {
            Assert.That(file.CreatedBy, Is.Not.EqualTo("System"),
                $"File({file.FileType}).CreatedBy must not be hardcoded 'System'");
            Assert.That(file.ModifiedBy, Is.Not.EqualTo("System"),
                $"File({file.FileType}).ModifiedBy must not be hardcoded 'System'");
        }
    }

    /// <summary>
    /// IDbContextFactory implementation for tests using InMemory database.
    /// </summary>
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
