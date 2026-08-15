using App.Core.DTOs.Fiscal;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Services.Seeders;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

namespace App.Services.Tests.Fiscal;

/// <summary>
/// Integration tests for FiscalCatalogSeeder backed by an EF Core in-memory database.
/// Verifies both catalogs are populated from the data reader and that seeding is
/// idempotent (running it again after the app restarts must not duplicate rows).
/// </summary>
[TestFixture]
[Category("Integration")]
public class FiscalCatalogSeederTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private Mock<IFiscalCatalogDataReader> _dataReaderMock = null!;
    private FiscalCatalogSeeder _sut = null!;

    private static readonly CreateRegimenFiscalCatalogoDto[] RegimenesDeMuestra =
    [
        new() { Codigo = "601", Descripcion = "General de Ley Personas Morales" },
        new() { Codigo = "612", Descripcion = "Personas Físicas con Actividades Empresariales y Profesionales" }
    ];

    private static readonly CreateUsoCfdiCatalogoDto[] UsosDeMuestra =
    [
        new() { Codigo = "G03", Descripcion = "Gastos en general.", CodigosRegimenFiscal = "601,612" },
        new() { Codigo = "S01", Descripcion = "Sin efectos fiscales.", CodigosRegimenFiscal = null }
    ];

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);

        _dataReaderMock = new Mock<IFiscalCatalogDataReader>();
        _dataReaderMock.Setup(r => r.GetRegimenesFiscalesAsync()).ReturnsAsync(RegimenesDeMuestra);
        _dataReaderMock.Setup(r => r.GetUsosCfdiAsync()).ReturnsAsync(UsosDeMuestra);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        _sut = new FiscalCatalogSeeder(
            _contextFactory,
            _dataReaderMock.Object,
            NullLogger<FiscalCatalogSeeder>.Instance,
            dateTimeMock.Object);
    }

    [Test]
    public async Task SeedAsync_PopulatesBothCatalogsFromReader()
    {
        await _sut.SeedAsync();

        await using var ctx = new ApplicationDbContext(_dbOptions);

        var regimenes = await ctx.RegimenesFiscalesCatalogo.ToListAsync();
        Assert.That(regimenes.Select(r => r.Codigo), Is.EquivalentTo(new[] { "601", "612" }));

        var usos = await ctx.UsosCfdiCatalogo.ToListAsync();
        Assert.That(usos.Select(u => u.Codigo), Is.EquivalentTo(new[] { "G03", "S01" }));

        var g03 = usos.Single(u => u.Codigo == "G03");
        Assert.That(g03.CodigosRegimenFiscal, Is.EqualTo("601,612"));
    }

    [Test]
    public async Task SeedAsync_SetsAuditFields()
    {
        await _sut.SeedAsync();

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var regimen = await ctx.RegimenesFiscalesCatalogo.FirstAsync();

        Assert.That(regimen.CreatedBy, Is.EqualTo("System"));
        Assert.That(regimen.CreatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateRows()
    {
        await _sut.SeedAsync();
        await _sut.SeedAsync();

        await using var ctx = new ApplicationDbContext(_dbOptions);

        Assert.That(await ctx.RegimenesFiscalesCatalogo.CountAsync(), Is.EqualTo(2),
            "Seeding twice must not duplicate the fiscal regimes catalog");
        Assert.That(await ctx.UsosCfdiCatalogo.CountAsync(), Is.EqualTo(2),
            "Seeding twice must not duplicate the CFDI uses catalog");
    }

    [Test]
    public async Task SeedAsync_AlreadySeededRegimenes_SkipsReaderForThatCatalog()
    {
        await using (var ctx = new ApplicationDbContext(_dbOptions))
        {
            ctx.RegimenesFiscalesCatalogo.Add(new App.Models.Fiscal.RegimenFiscalCatalogo
            {
                Codigo = "999",
                Descripcion = "Pre-seeded",
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        await _sut.SeedAsync();

        await using var verifyCtx = new ApplicationDbContext(_dbOptions);
        var regimenes = await verifyCtx.RegimenesFiscalesCatalogo.ToListAsync();

        Assert.That(regimenes, Has.Count.EqualTo(1),
            "When the catalog already has rows, the seeder must not insert the reader's data on top");
        Assert.That(regimenes.Single().Codigo, Is.EqualTo("999"));
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
