using App.Models.Data.Contexts;
using App.Models.Fiscal;
using App.Services.Fiscal;
using App.Services.Mappings;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

namespace App.Services.Tests.Fiscal;

/// <summary>
/// Integration tests for FiscalCatalogService backed by an EF Core in-memory database.
/// Covers ordering and the Régimen Fiscal → Uso de CFDI filtering that drives the
/// dependent dropdown in ClienteDialog.
/// </summary>
[TestFixture]
[Category("Integration")]
public class FiscalCatalogServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private FiscalCatalogService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<FiscalCatalogMappingProfile>());
        var mapper = mapperConfig.CreateMapper();

        _sut = new FiscalCatalogService(
            _contextFactory,
            mapper,
            NullLogger<FiscalCatalogService>.Instance);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task SeedRegimenesAsync(params (string Codigo, string Descripcion)[] regimenes)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        foreach (var (codigo, descripcion) in regimenes)
        {
            ctx.RegimenesFiscalesCatalogo.Add(new RegimenFiscalCatalogo
            {
                Codigo = codigo,
                Descripcion = descripcion,
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    private async Task SeedUsosCfdiAsync(params (string Codigo, string Descripcion, string? CodigosRegimenFiscal)[] usos)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        foreach (var (codigo, descripcion, codigosRegimen) in usos)
        {
            ctx.UsosCfdiCatalogo.Add(new UsoCfdiCatalogo
            {
                Codigo = codigo,
                Descripcion = descripcion,
                CodigosRegimenFiscal = codigosRegimen,
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    // ── GetRegimenesFiscalesAsync / GetUsosCfdiAsync ─────────────────────────

    [Test]
    public async Task GetRegimenesFiscalesAsync_ReturnsOrderedByCodigo()
    {
        await SeedRegimenesAsync(
            ("626", "Régimen Simplificado de Confianza"),
            ("601", "General de Ley Personas Morales"),
            ("612", "Personas Físicas con Actividades Empresariales y Profesionales"));

        var result = await _sut.GetRegimenesFiscalesAsync();

        Assert.That(result.Select(r => r.Codigo), Is.EqualTo(new[] { "601", "612", "626" }));
    }

    [Test]
    public async Task GetUsosCfdiAsync_ReturnsOrderedByCodigo()
    {
        await SeedUsosCfdiAsync(
            ("S01", "Sin efectos fiscales.", null),
            ("G01", "Adquisición de mercancías.", "601,612"),
            ("CN01", "Nómina", "605"));

        var result = await _sut.GetUsosCfdiAsync();

        Assert.That(result.Select(u => u.Codigo), Is.EqualTo(new[] { "CN01", "G01", "S01" }));
    }

    // ── GetUsosCfdiPorRegimenAsync ───────────────────────────────────────────

    [Test]
    public async Task GetUsosCfdiPorRegimenAsync_ReturnsOnlyUsosApplicableToRegimen()
    {
        await SeedUsosCfdiAsync(
            ("G01", "Adquisición de mercancías.", "601,612,626"),
            ("D01", "Honorarios médicos.", "605,606,612"),
            ("CN01", "Nómina", "605"));

        var result = await _sut.GetUsosCfdiPorRegimenAsync("612");

        Assert.That(result.Select(u => u.Codigo), Is.EquivalentTo(new[] { "G01", "D01" }));
        Assert.That(result.Select(u => u.Codigo), Does.Not.Contain("CN01"),
            "CN01 only applies to régimen 605 (Nómina), not 612");
    }

    [Test]
    public async Task GetUsosCfdiPorRegimenAsync_IncludesUsosWithNullRegimenCodes()
    {
        // CodigosRegimenFiscal == null means "applies to all regimes" (matches S01/CP01 in the real catalog).
        await SeedUsosCfdiAsync(
            ("S01", "Sin efectos fiscales.", null),
            ("G01", "Adquisición de mercancías.", "601"));

        var result = await _sut.GetUsosCfdiPorRegimenAsync("626");

        Assert.That(result.Select(u => u.Codigo), Is.EquivalentTo(new[] { "S01" }));
    }

    [Test]
    public async Task GetUsosCfdiPorRegimenAsync_RegimenWithNoMatches_ReturnsOnlyUniversalUsos()
    {
        await SeedUsosCfdiAsync(
            ("S01", "Sin efectos fiscales.", null),
            ("CN01", "Nómina", "605"));

        var result = await _sut.GetUsosCfdiPorRegimenAsync("999");

        Assert.That(result.Select(u => u.Codigo), Is.EquivalentTo(new[] { "S01" }));
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
