using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Billing;
using App.Core.DTOs.Billing.Mexico;
using App.Core.DTOs.Settings;
using App.Core.Enums.Billing;
using App.Core.Enums.Shop;
using App.Core.Constants;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Models.Cfdi.V40;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Models.Shared;
using App.Services.Billing;
using App.Services.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace App.Services.Tests.Billing;

/// <summary>
/// Tests that GlobalInvoiceService.CreateAndStampAsync correctly groups sales by IVA status
/// and builds CFDI Conceptos with exact (configured) tax rates — never blended/derived averages.
///
/// Scenarios covered:
///   1. All taxable sales  → 1 Concepto (ObjetoImp=02) with TasaOCuota = configured rate
///   2. All exempt sales   → 1 Concepto (ObjetoImp=01) with no Impuestos node
///   3. Mixed sales        → 2 Conceptos: one taxable, one exempt
///   4. Configured rate used → ITaxRateService is called; derived/blended rate is NOT used
///   5. Comprobante totals   → SubTotal/Descuento/Total aggregate all sales regardless of tax status
/// </summary>
[TestFixture]
public class GlobalInvoiceTaxGroupingTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private Mock<IMexicoCfdiXmlService> _xmlServiceMock = null!;
    private Mock<IMexicoCsdSigningService> _signingMock = null!;
    private Mock<ISwSapienService> _pacServiceMock = null!;
    private Mock<IMexicoPacSettingsService> _pacSettingsMock = null!;
    private Mock<ITaxSettingsService> _taxSettingsMock = null!;
    private Mock<ITaxRateService> _taxRateMock = null!;
    private Mock<ICompanySettingsService> _companyMock = null!;
    private Mock<ICurrentUserService> _currentUserMock = null!;
    private Mock<IDateTime> _dateTimeMock = null!;

    /// <summary>Last Comprobante passed to GenerateXmlAsync, captured via mock callback.</summary>
    private Comprobante? _capturedComprobante;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .Options;

        _capturedComprobante = null;

        // XML service: capture the Comprobante so tests can assert on its structure
        _xmlServiceMock = new Mock<IMexicoCfdiXmlService>();
        _xmlServiceMock
            .Setup(x => x.GenerateXmlAsync(It.IsAny<Comprobante>()))
            .Callback<Comprobante>(c => _capturedComprobante = c)
            .ReturnsAsync(Result<string>.Success("<xml/>"));

        // Signing service: returns dummy signed XML
        _signingMock = new Mock<IMexicoCsdSigningService>();
        _signingMock
            .Setup(x => x.SignXmlAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(Result<string>.Success("<signed/>"));

        // PAC stamp: returns a minimal valid stamp result
        _pacServiceMock = new Mock<ISwSapienService>();
        _pacServiceMock
            .Setup(x => x.StampAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<SwSapienStampData>.Success(new SwSapienStampData
            {
                Uuid = Guid.NewGuid().ToString(),
                SelloCfdi = "sello_cfdi",
                SelloSat = "sello_sat"
            }));

        // PAC settings: configured (CSD + token present)
        _pacSettingsMock = new Mock<IMexicoPacSettingsService>();
        _pacSettingsMock.Setup(x => x.GetAsync())
            .ReturnsAsync(new MexicoPacSettingsDto
            {
                HasCsdCertificate = true,
                HasCsdPrivateKey = true,
                HasToken = true,
                GlobalInvoiceSerie = "G",
                GlobalInvoiceStartFolio = 1,
                GlobalInvoiceFolioLength = 0
            });
        _pacSettingsMock.Setup(x => x.GetCsdCertificateBytesAsync())
            .ReturnsAsync(Result<byte[]>.Success(new byte[] { 1 }));
        _pacSettingsMock.Setup(x => x.GetCsdPrivateKeyBytesAsync())
            .ReturnsAsync(Result<byte[]>.Success(new byte[] { 2 }));
        _pacSettingsMock.Setup(x => x.GetCsdPasswordAsync())
            .ReturnsAsync(Result<string>.Success("pwd"));

        // Tax settings: Mexico
        _taxSettingsMock = new Mock<ITaxSettingsService>();
        _taxSettingsMock.Setup(x => x.GetSettingsAsync())
            .ReturnsAsync(new TaxSettingsDto
            {
                CountryCode = "MX",
                TaxId = "XAXX010101000",
                BusinessName = "EMPRESA TEST SA",
                FiscalRegime = "601",
                PostalCode = "64000"
            });

        // Tax rate service: 16% IVA (0.160000)
        _taxRateMock = new Mock<ITaxRateService>();
        _taxRateMock
            .Setup(x => x.GetEffectiveRateAsync(It.IsAny<string>(), null, null))
            .ReturnsAsync(0.16m);

        // Company: UTC timezone (simplifies date math in tests)
        _companyMock = new Mock<ICompanySettingsService>();
        _companyMock.Setup(x => x.GetCurrentTimeZoneAsync())
            .ReturnsAsync(TimeZoneInfo.Utc);

        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.UserId).Returns("test-user");

        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 1 — All taxable sales
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task AllTaxableSales_OneConcepto_ObjetoImp02_WithExactConfiguredRate()
    {
        await SeedSalesAsync(
            (subtotal: 100m, tax: 16m, discount: 0m),
            (subtotal: 200m, tax: 32m, discount: 0m));

        var result = await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedComprobante, Is.Not.Null);

        var conceptos = _capturedComprobante!.Conceptos;
        Assert.That(conceptos, Has.Count.EqualTo(1), "Expected exactly one Concepto for all-taxable sales");

        var c = conceptos[0];
        Assert.That(c.ObjetoImp, Is.EqualTo("02"), "Concepto should be taxable (ObjetoImp=02)");
        Assert.That(c.Impuestos, Is.Not.Null, "Taxable Concepto must have Impuestos");
        Assert.That(c.Impuestos!.Traslados, Has.Count.EqualTo(1));

        var traslado = c.Impuestos.Traslados![0];
        Assert.That(traslado.TasaOCuota, Is.EqualTo(0.16m),
            "TasaOCuota must be the exact configured rate (0.16), not a derived value");
        Assert.That(traslado.Impuesto, Is.EqualTo("002"), "IVA code must be 002");

        // Comprobante-level Impuestos must exist and reflect the total IVA
        Assert.That(_capturedComprobante.Impuestos, Is.Not.Null);
        Assert.That(_capturedComprobante.Impuestos!.TotalImpuestosTrasladados,
            Is.EqualTo(48m), "Total IVA should be 16+32=48");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 2 — All exempt sales
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task AllExemptSales_OneConcepto_ObjetoImp01_NoImpuestos()
    {
        await SeedSalesAsync(
            (subtotal: 100m, tax: 0m, discount: 0m),
            (subtotal: 50m,  tax: 0m, discount: 0m));

        var result = await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedComprobante, Is.Not.Null);

        var conceptos = _capturedComprobante!.Conceptos;
        Assert.That(conceptos, Has.Count.EqualTo(1), "Expected exactly one Concepto for all-exempt sales");

        var c = conceptos[0];
        Assert.That(c.ObjetoImp, Is.EqualTo("01"), "Exempt Concepto must have ObjetoImp=01");
        Assert.That(c.Impuestos, Is.Null, "Exempt Concepto must not have an Impuestos node");

        // No IVA at Comprobante level
        Assert.That(_capturedComprobante.Impuestos, Is.Null,
            "Comprobante Impuestos must be absent when all sales are exempt");

        // ITaxRateService should NOT be called — there are no taxable sales
        _taxRateMock.Verify(
            x => x.GetEffectiveRateAsync(It.IsAny<string>(), null, null),
            Times.Never,
            "GetEffectiveRateAsync should not be called when there are no taxable sales");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 3 — Mixed sales
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task MixedSales_TwoConceptos_OnePerTaxGroup()
    {
        await SeedSalesAsync(
            (subtotal: 100m, tax: 16m, discount: 0m),   // taxable
            (subtotal: 200m, tax: 32m, discount: 0m),   // taxable
            (subtotal: 50m,  tax: 0m,  discount: 0m));  // exempt

        var result = await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedComprobante, Is.Not.Null);

        var conceptos = _capturedComprobante!.Conceptos;
        Assert.That(conceptos, Has.Count.EqualTo(2), "Expected two Conceptos: one taxable, one exempt");

        var taxable = conceptos.Single(c => c.ObjetoImp == "02");
        var exempt  = conceptos.Single(c => c.ObjetoImp == "01");

        // Taxable concepto: correct amounts and exact rate
        Assert.That(taxable.ValorUnitario, Is.EqualTo(300m), "Taxable subtotal should be 100+200=300");
        Assert.That(taxable.Impuestos?.Traslados?[0].TasaOCuota, Is.EqualTo(0.16m));

        // Exempt concepto: no Impuestos
        Assert.That(exempt.ValorUnitario, Is.EqualTo(50m), "Exempt subtotal should be 50");
        Assert.That(exempt.Impuestos, Is.Null);

        // Comprobante Impuestos only reflects the taxable portion (48 = 16+32)
        Assert.That(_capturedComprobante.Impuestos?.TotalImpuestosTrasladados, Is.EqualTo(48m));
        Assert.That(_capturedComprobante.Impuestos!.Traslados![0].TasaOCuota, Is.EqualTo(0.16m));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 4 — Configured rate is used, not a derived/blended rate
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task TaxRate_ComesFromITaxRateService_NotDerivedFromData()
    {
        // With these sales, a naive derived rate = totalTax / totalBase = 16 / 200 = 0.08.
        // The correct behaviour must use ITaxRateService (which returns 0.16) and
        // produce two Conceptos, NOT a single one with the blended 0.08 rate.
        await SeedSalesAsync(
            (subtotal: 100m, tax: 16m, discount: 0m),  // taxable
            (subtotal: 100m, tax: 0m,  discount: 0m)); // exempt → blended would be 0.08

        var result = await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);

        // ITaxRateService called exactly once, with the company country code
        _taxRateMock.Verify(
            x => x.GetEffectiveRateAsync("MX", null, null),
            Times.Once,
            "GetEffectiveRateAsync must be called once with the company country code");

        var taxableConcept = _capturedComprobante?.Conceptos.Single(c => c.ObjetoImp == "02");
        Assert.That(taxableConcept, Is.Not.Null);
        Assert.That(
            taxableConcept!.Impuestos!.Traslados![0].TasaOCuota,
            Is.EqualTo(0.16m),
            "TasaOCuota must be 0.16 (from ITaxRateService), not 0.08 (derived/blended)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 5 — Comprobante-level totals aggregate all sales
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ComprobanteLevel_Totals_AggregateAllSalesRegardlessOfTaxStatus()
    {
        // taxable: sub=100, disc=10  → taxBase=90, tax=Round(90×0.16,2)=14.40
        // exempt:  sub=50,  disc=5   → tax=0
        // Comprobante:
        //   SubTotal  = 100 + 50 = 150        (both groups)
        //   Descuento = 10  + 5  = 15         (both groups)
        //   Tax       = 14.40                 (fiscal, from base)
        //   Total     = 150 - 15 + 14.40 = 149.40  (SAT CFDI40119 formula)
        await SeedSalesAsync(
            (subtotal: 100m, tax: 14.40m, discount: 10m),
            (subtotal: 50m,  tax: 0m,     discount: 5m));

        await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(_capturedComprobante, Is.Not.Null);
        Assert.That(_capturedComprobante!.SubTotal, Is.EqualTo(150m),   "SubTotal must include both taxable and exempt groups");
        Assert.That(_capturedComprobante.Descuento, Is.EqualTo(15m),    "Descuento must include both taxable and exempt groups");
        Assert.That(_capturedComprobante.Total,     Is.EqualTo(149.40m),"Total = SubTotal - Descuento + Tax (CFDI40119)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 6 — CFDI40221: comprobante Importe == sum of concept Importes (rounding)
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ComprobanteTraslado_Importe_MatchesConceptTraslado_Importe_WhenRoundingAccumulates()
    {
        // Two sales where individual taxes (rounded to 2 dp) sum to a different value
        // than the tax recomputed from the aggregated base.
        //
        //   Sale 1: subtotal=6.05  → tax stored = Round(6.05 × 0.16, 2) = Round(0.968,  2) = 0.97
        //   Sale 2: subtotal=6.04  → tax stored = Round(6.04 × 0.16, 2) = Round(0.9664, 2) = 0.97
        //   DB sum:  0.97 + 0.97 = 1.94          ← old code used this → CFDI40221 rejection
        //   Recalc:  base=12.09, 12.09 × 0.16 = 1.9344 → Round = 1.93  ← concept uses this
        //
        // SAT CFDI40221: Comprobante.Traslado.Importe must equal Round(Sum(Concepto.Traslado.Importe), 2)
        await SeedSalesAsync(
            (subtotal: 6.05m, tax: 0.97m, discount: 0m),
            (subtotal: 6.04m, tax: 0.97m, discount: 0m));

        var result = await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedComprobante, Is.Not.Null);

        var conceptTraslado    = _capturedComprobante!.Conceptos[0].Impuestos!.Traslados![0];
        var comprobanteTraslado = _capturedComprobante.Impuestos!.Traslados![0];

        Assert.That(
            Math.Round(comprobanteTraslado.Importe, 2),
            Is.EqualTo(Math.Round(conceptTraslado.Importe, 2)),
            "Comprobante.Traslado.Importe must equal Concepto.Traslado.Importe (CFDI40221)");

        Assert.That(
            comprobanteTraslado.Importe,
            Is.EqualTo(1.93m),
            "Importe must be 1.93 (recomputed from base), not 1.94 (sum of individually-rounded DB values)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 7 — CFDI40119: Total == SubTotal - Descuento + TotalImpuestosTrasladados
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ComprobanteTotal_EqualsSubtotalMinusDescuentoPlusTax_WhenRoundingAccumulates()
    {
        // Same sales as test 6 — DB totals sum to 14.00 but fiscal total must be 13.99.
        //
        //   sale1: subtotal=6.05, tax=0.97, total=7.02  (stored individually)
        //   sale2: subtotal=6.04, tax=0.97, total=7.01
        //   DB totals sum: 14.03... wait let me recalc
        //
        //   Actually: subtotal=12.09, conceptoTaxRounded=1.93
        //   Fiscal total = 12.09 - 0 + 1.93 = 14.02
        //   But DB sum of totals = 7.02 + 7.01 = 14.03 — diverges
        //
        // SAT CFDI40119: Total = SubTotal - Descuento + TotalImpuestosTrasladados
        await SeedSalesAsync(
            (subtotal: 6.05m, tax: 0.97m, discount: 0m),
            (subtotal: 6.04m, tax: 0.97m, discount: 0m));

        var result = await BuildService().CreateAndStampAsync(MakeDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(_capturedComprobante, Is.Not.Null);

        var c = _capturedComprobante!;
        var taxImporte = c.Impuestos?.Traslados?[0].Importe ?? 0m;
        var expectedTotal = Math.Round(c.SubTotal - c.Descuento + taxImporte, 2);

        Assert.That(
            c.Total,
            Is.EqualTo(expectedTotal),
            $"Total ({c.Total}) must equal SubTotal - Descuento + Tax ({expectedTotal}) (CFDI40119)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 8 — PreviewAsync totals match CFDI totals (same aggregated recalculation)
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task PreviewAsync_TaxAndTotal_MatchCfdiAggregatedRecalculation_WhenRoundingAccumulates()
    {
        // Two sales of $7.00 (subtotal=6.03, tax=0.97 each).
        //   DB tax sum:  0.97 + 0.97 = 1.94  ← old preview showed this
        //   CFDI recalc: base=12.06, 12.06×0.16=1.9296 → Round=1.93
        //   Expected preview TaxAmount = 1.93, Total = 12.06 - 0 + 1.93 = 13.99
        await SeedSalesAsync(
            (subtotal: 6.03m, tax: 0.97m, discount: 0m),
            (subtotal: 6.03m, tax: 0.97m, discount: 0m));

        var result = await BuildService().PreviewAsync(
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 23, 59, 59, DateTimeKind.Utc));

        Assert.That(result.IsSuccess, Is.True, result.Error);
        var preview = result.Value!;

        Assert.That(preview.TaxAmount, Is.EqualTo(1.93m),
            "Preview TaxAmount must use aggregated base recalculation (1.93), not DB sum (1.94)");
        Assert.That(preview.Total, Is.EqualTo(13.99m),
            "Preview Total must equal SubTotal - Discount + TaxAmount (13.99), not DB sum (14.00)");
        Assert.That(preview.Total, Is.EqualTo(preview.Subtotal - preview.DiscountAmount + preview.TaxAmount),
            "Preview Total must satisfy SubTotal - Discount + Tax (CFDI40119 formula)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private GlobalInvoiceService BuildService()
    {
        var localizer = new Mock<IStringLocalizer<GlobalInvoiceService>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));

        return new GlobalInvoiceService(
            contextFactory: new TestDbContextFactory(_dbOptions),
            xmlService: _xmlServiceMock.Object,
            signingService: _signingMock.Object,
            pacService: _pacServiceMock.Object,
            pacSettingsService: _pacSettingsMock.Object,
            taxSettingsService: _taxSettingsMock.Object,
            taxRateService: _taxRateMock.Object,
            companySettingsService: _companyMock.Object,
            pdfService: new Mock<IPdfService>().Object,
            emailTemplateService: new Mock<IEmailTemplateService>().Object,
            fiscalCatalogService: new Mock<IMexicoFiscalCatalogService>().Object,
            currentUserService: _currentUserMock.Object,
            dateTime: _dateTimeMock.Object,
            applicationOptions: Options.Create(new ApplicationOptions { Name = "Test", BaseUrl = "http://localhost" }),
            localizer: localizer.Object,
            logger: NullLogger<GlobalInvoiceService>.Instance);
    }

    /// <summary>Seeds public/created sales into the in-memory DB. All dated 2026-04-01 12:00 UTC.</summary>
    private async Task SeedSalesAsync(params (decimal subtotal, decimal tax, decimal discount)[] sales)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);

        ctx.Customers.Add(new Customer
        {
            Id = 1,
            Name = "Test Customer",
            Email = "test@test.com",
            CountryCode = "MX",
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "test",
            ModifiedAt = DateTime.UtcNow
        });

        var baseDate = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < sales.Length; i++)
        {
            var (sub, tax, disc) = sales[i];
            ctx.Sales.Add(new Sale
            {
                CustomerId = 1,
                SaleType = SaleType.Public,
                Status = App.Core.Enums.Shop.SaleStatus.Created,
                SaleDate = baseDate.AddMinutes(i),
                Subtotal = sub,
                TaxAmount = tax,
                DiscountAmount = disc,
                Total = sub + tax - disc,
                CreatedBy = "test",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "test",
                ModifiedAt = DateTime.UtcNow
            });
        }

        await ctx.SaveChangesAsync();
    }

    private static CreateGlobalInvoiceDto MakeDto() => new()
    {
        StartDate = new DateTime(2026, 4, 1),
        EndDate = new DateTime(2026, 4, 1),
        Periodicity = GlobalInvoicePeriodicity.Daily,
        PaymentForm = "01"
    };

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}
