using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Obras;
using App.Services.Obras;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Moq;

using NUnit.Framework;

namespace App.Services.Tests.Obras;

/// <summary>
/// Integration tests for ObraFolioService backed by an EF Core in-memory database. Mirrors
/// CotizacionFolioServiceTests — same per-año sequential numbering scheme, applied to Obra instead
/// of Cotizacion (see CotizacionFolioFormatter for the shared display formatting, reused as-is).
/// </summary>
[TestFixture]
[Category("Integration")]
public class ObraFolioServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private Mock<IDateTime> _dateTimeMock = null!;
    private Mock<ICompanySettingsService> _companySettingsMock = null!;
    private ObraFolioService _sut = null!;

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

        _sut = new ObraFolioService(
            _contextFactory,
            _dateTimeMock.Object,
            _companySettingsMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void SetNow(DateTime utcNow) => _dateTimeMock.Setup(d => d.Now).Returns(utcNow);

    private async Task SeedObraAsync(int? folioAnio, int? folioNumero)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Obras.Add(new Obra
        {
            ClienteId = 1,
            Direccion = "Calle Falsa 123",
            Estado = ObraEstado.Solicitada,
            FechaSolicitud = DateTime.UtcNow,
            FolioAnio = folioAnio,
            FolioNumero = folioNumero,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    // ── GenerarSiguienteFolioAsync ────────────────────────────────────────────

    [Test]
    public async Task GenerarSiguienteFolioAsync_NoExistingObras_ReturnsNumeroOne()
    {
        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_ExistingObrasSameAnio_IncrementsFromMax()
    {
        await SeedObraAsync(2026, 1);
        await SeedObraAsync(2026, 5);

        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(6), "Must continue from the highest existing número, not the count");
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_ExistingObrasOtherAnio_DoesNotAffectSequence()
    {
        await SeedObraAsync(2025, 42);

        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2026));
        Assert.That(numero, Is.EqualTo(1), "Each año must have its own independent sequence");
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_NewAnio_ResetsToOne()
    {
        await SeedObraAsync(2026, 10);

        SetNow(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var (anio, numero) = await _sut.GenerarSiguienteFolioAsync();

        Assert.That(anio, Is.EqualTo(2027));
        Assert.That(numero, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerarSiguienteFolioAsync_LegacyObrasWithoutFolio_AreIgnored()
    {
        // Obras created before the folio feature existed have FolioAnio/FolioNumero == null.
        await SeedObraAsync(null, null);

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
