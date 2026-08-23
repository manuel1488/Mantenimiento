using App.Core.Interfaces;
using App.Models.Cotizaciones;
using App.Models.Data.Contexts;
using App.Services.Cotizaciones;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Moq;

using NUnit.Framework;

namespace App.Services.Tests.Cotizaciones;

/// <summary>
/// Integration tests for CotizacionFolioService backed by an EF Core in-memory database.
/// Covers the per-año sequential numbering (starts at 1, increments from the max, resets on a
/// new año) that backs a Cotización's folio (see CotizacionFolioFormatter for display formatting).
/// </summary>
[TestFixture]
[Category("Integration")]
public class CotizacionFolioServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private Mock<IDateTime> _dateTimeMock = null!;
    private Mock<ICompanySettingsService> _companySettingsMock = null!;
    private CotizacionFolioService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);

        _dateTimeMock = new Mock<IDateTime>();
        SetNow(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _companySettingsMock = new Mock<ICompanySettingsService>();
        _companySettingsMock.Setup(s => s.GetCurrentTimeZoneAsync()).ReturnsAsync(TimeZoneInfo.Utc);

        _sut = new CotizacionFolioService(
            _contextFactory,
            _dateTimeMock.Object,
            _companySettingsMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void SetNow(DateTime utcNow) => _dateTimeMock.Setup(d => d.Now).Returns(utcNow);

    private async Task SeedCotizacionAsync(int folioAnio, int folioNumero)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Cotizaciones.Add(new Cotizacion
        {
            ClienteId = 1,
            FechaGeneracion = DateTime.UtcNow,
            FolioAnio = folioAnio,
            FolioNumero = folioNumero,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    // ── GenerarSiguienteFolioAsync ────────────────────────────────────────────

    [Test]
    public async Task GenerarSiguienteFolioAsync_NoExistingCotizaciones_ReturnsNumeroOne()
    {
        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_ExistingCotizacionesSameAnio_IncrementsFromMax()
    {
        await SeedCotizacionAsync(2026, 1);
        await SeedCotizacionAsync(2026, 5);

        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(6), "Must continue from the highest existing número, not the count");
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_ExistingCotizacionesOtherAnio_DoesNotAffectSequence()
    {
        await SeedCotizacionAsync(2025, 42);

        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(1), "Each año must have its own independent sequence");
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_NewAnio_ResetsToOne()
    {
        await SeedCotizacionAsync(2026, 10);

        SetNow(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2027));
        Assert.That(numero, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_LegacyCotizacionesWithoutFolio_AreIgnored()
    {
        // Cotizaciones created before the folio feature existed have FolioAnio/FolioNumero == null.
        await using (var ctx = new ApplicationDbContext(_dbOptions))
        {
            ctx.Cotizaciones.Add(new Cotizacion
            {
                ClienteId = 1,
                FechaGeneracion = DateTime.UtcNow,
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_UsesConfiguredCompanyTimeZone_NotUtc()
    {
        // 2026-01-01 00:30 UTC is still 2025-12-31 evening in a UTC-6 timezone — the folio año
        // must follow the company's local calendar date, not the raw UTC date.
        SetNow(new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Utc));
        _companySettingsMock.Setup(s => s.GetCurrentTimeZoneAsync())
            .ReturnsAsync(TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"));

        var (anio, _) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2025));
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new(_options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken _ = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
