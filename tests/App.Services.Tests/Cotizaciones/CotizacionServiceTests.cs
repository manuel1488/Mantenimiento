using App.Core.Common;
using App.Core.DTOs.Cotizaciones;
using App.Core.Enums.Cotizaciones;
using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Clientes;
using App.Models.Cotizaciones;
using App.Models.Data.Contexts;
using App.Models.Servicios;
using App.Services.Cotizaciones;
using App.Services.Mappings;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

namespace App.Services.Tests.Cotizaciones;

/// <summary>
/// Integration tests for CotizacionService backed by an EF Core in-memory database.
/// Covers the CreateAsync/UpdateAsync totals calculation — specifically a regression where
/// UpdateAsync recalculated Subtotal/IvaMonto/Total from dto.IncluirIva but never persisted the
/// IncluirIva flag itself on the entity, so editing a Cotización to turn IVA on left the stored
/// Total IVA-inclusive while IncluirIva (and therefore the "IVA" line on the detail page) stayed
/// false — see CLAUDE session notes for the reported incident.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CotizacionServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private CotizacionService _sut = null!;
    private const decimal IvaTasaPorDefecto = 16m;

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CotizacionMappingProfile>());
        var mapper = mapperConfig.CreateMapper();

        var localizerMock = new Mock<IStringLocalizer<CotizacionService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, key));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync("test-user");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new App.Core.DTOs.Settings.CompanySettingsDto { IvaTasaPorDefecto = IvaTasaPorDefecto });

        var integrityHashMock = new Mock<ICotizacionIntegrityHashService>();
        integrityHashMock.Setup(h => h.Compute(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<DateTime>(),
                It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<IEnumerable<CotizacionIntegrityLinea>>()))
            .Returns("test-hash");

        var folioMock = new Mock<ICotizacionFolioService>();
        folioMock.Setup(f => f.GenerarSiguienteFolioAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime.UtcNow.Year, 1));

        _sut = new CotizacionService(
            _contextFactory,
            mapper,
            NullLogger<CotizacionService>.Instance,
            localizerMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            Mock.Of<IPdfService>(),
            Mock.Of<ICotizacionTemplateSettingsService>(),
            companySettingsMock.Object,
            Mock.Of<IEmailTemplateService>(),
            Mock.Of<IEmailService>(),
            Mock.Of<IFiscalCatalogService>(),
            folioMock.Object,
            integrityHashMock.Object,
            Options.Create(new ApplicationOptions()),
            Options.Create(new BrandingOptions()),
            Mock.Of<IImageService>(),
            Mock.Of<IFileStorageService>(),
            Options.Create(new CotizacionFotoOptions()));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<(int ClienteId, int ServicioId)> SeedClienteYServicioAsync()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);

        var unidad = new UnidadMedida
        {
            Codigo = "M2",
            Nombre = "Metro cuadrado",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };
        ctx.UnidadesMedida.Add(unidad);

        var cliente = new Cliente
        {
            Nombre = "Cliente de Prueba",
            Pais = "Mexico",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };
        ctx.Clientes.Add(cliente);

        await ctx.SaveChangesAsync();

        var servicio = new Servicio
        {
            Nombre = "Servicio de Prueba",
            UnidadMedidaId = unidad.Id,
            PrecioUnitario = 100m,
            RendimientoDiasPorUnidad = 1m,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };
        ctx.Servicios.Add(servicio);

        await ctx.SaveChangesAsync();

        return (cliente.Id, servicio.Id);
    }

    private async Task<int> SeedCotizacionAsync(int clienteId, int servicioId, bool incluirIva)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);

        var subtotal = 1000m;
        var ivaMonto = incluirIva ? Math.Round(subtotal * IvaTasaPorDefecto / 100m, 2) : 0m;

        var cotizacion = new Cotizacion
        {
            ClienteId = clienteId,
            FechaGeneracion = DateTime.UtcNow,
            Estado = CotizacionEstado.Pendiente,
            IncluirIva = incluirIva,
            Subtotal = subtotal,
            IvaTasa = incluirIva ? IvaTasaPorDefecto : 0m,
            IvaMonto = ivaMonto,
            Total = subtotal + ivaMonto,
            IntegridadHash = "seed-hash",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };
        cotizacion.Lineas.Add(new CotizacionLinea
        {
            ServicioId = servicioId,
            ServicioNombre = "Servicio de Prueba",
            UnidadMedida = "M2",
            Cantidad = 10m,
            PrecioUnitario = 100m,
            Subtotal = subtotal,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });

        ctx.Cotizaciones.Add(cotizacion);
        await ctx.SaveChangesAsync();

        return cotizacion.Id;
    }

    // ── UpdateAsync: IncluirIva persistence (regression) ─────────────────────

    [Test]
    public async Task UpdateAsync_TurningIncluirIvaOn_PersistsFlagAndRecalculatesTotal()
    {
        var (clienteId, servicioId) = await SeedClienteYServicioAsync();
        var cotizacionId = await SeedCotizacionAsync(clienteId, servicioId, incluirIva: false);

        var dto = new UpdateCotizacionDto
        {
            ClienteId = clienteId,
            IncluirIva = true,
            Lineas = [new CreateCotizacionLineaDto { ServicioId = servicioId, Cantidad = 10m, PrecioUnitarioOverride = 100m }]
        };

        var result = await _sut.UpdateAsync(cotizacionId, dto);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.IncluirIva, Is.True, "IncluirIva must be persisted as true on the returned DTO");
        Assert.That(result.Value.IvaMonto, Is.EqualTo(160m));
        Assert.That(result.Value.Total, Is.EqualTo(1160m));

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var entity = await ctx.Cotizaciones.FirstAsync(c => c.Id == cotizacionId);
        Assert.That(entity.IncluirIva, Is.True,
            "Regression: IncluirIva must be written on the entity, not just used transiently to compute totals");
        Assert.That(entity.IvaMonto, Is.EqualTo(160m));
        Assert.That(entity.Total, Is.EqualTo(1160m));
    }

    [Test]
    public async Task UpdateAsync_TurningIncluirIvaOff_PersistsFlagAndRecalculatesTotal()
    {
        var (clienteId, servicioId) = await SeedClienteYServicioAsync();
        var cotizacionId = await SeedCotizacionAsync(clienteId, servicioId, incluirIva: true);

        var dto = new UpdateCotizacionDto
        {
            ClienteId = clienteId,
            IncluirIva = false,
            Lineas = [new CreateCotizacionLineaDto { ServicioId = servicioId, Cantidad = 10m, PrecioUnitarioOverride = 100m }]
        };

        var result = await _sut.UpdateAsync(cotizacionId, dto);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.IncluirIva, Is.False);
        Assert.That(result.Value.IvaMonto, Is.EqualTo(0m));
        Assert.That(result.Value.Total, Is.EqualTo(1000m));

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var entity = await ctx.Cotizaciones.FirstAsync(c => c.Id == cotizacionId);
        Assert.That(entity.IncluirIva, Is.False);
        Assert.That(entity.IvaMonto, Is.EqualTo(0m));
        Assert.That(entity.Total, Is.EqualTo(1000m));
    }

    [Test]
    public async Task CreateAsync_IncluirIvaTrue_SetsFlagAndTotal()
    {
        var (clienteId, servicioId) = await SeedClienteYServicioAsync();

        var dto = new CreateCotizacionDto
        {
            ClienteId = clienteId,
            IncluirIva = true,
            Lineas = [new CreateCotizacionLineaDto { ServicioId = servicioId, Cantidad = 10m, PrecioUnitarioOverride = 100m }]
        };

        var result = await _sut.CreateAsync(dto);

        Assert.That(result.IsSuccess, Is.True, result.Error);

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var entity = await ctx.Cotizaciones.FirstAsync(c => c.Id == result.Value!.Id);
        Assert.That(entity.IncluirIva, Is.True);
        Assert.That(entity.IvaMonto, Is.EqualTo(160m));
        Assert.That(entity.Total, Is.EqualTo(1160m));
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
