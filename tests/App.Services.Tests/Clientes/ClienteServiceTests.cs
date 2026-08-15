using App.Core.DTOs.Clientes;
using App.Core.Interfaces;
using App.Models.Clientes;
using App.Models.Data.Contexts;
using App.Models.Obras;
using App.Services.Clientes;
using App.Services.Mappings;
using App.Shared.Services;

using AutoMapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

namespace App.Services.Tests.Clientes;

/// <summary>
/// Integration tests for ClienteService backed by an EF Core in-memory database.
/// Covers RFC uniqueness, the conditional Razón Social requirement (TieneDatosFiscales),
/// and the Obras guard on delete.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ClienteServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private ClienteService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<ClienteMappingProfile>());
        var mapper = mapperConfig.CreateMapper();

        var localizerMock = new Mock<IStringLocalizer<ClienteService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetUserIdAsync()).ReturnsAsync("test-user");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        _sut = new ClienteService(
            _contextFactory,
            mapper,
            NullLogger<ClienteService>.Instance,
            localizerMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static CreateClienteDto ValidCreateDto(string? rfc = null) => new()
    {
        Nombre = "Cliente de Prueba",
        Pais = "Mexico",
        Rfc = rfc
    };

    private async Task<int> SeedClienteAsync(string nombre = "Cliente Existente", string? rfc = null)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var cliente = new Cliente
        {
            Nombre = nombre,
            Pais = "Mexico",
            Rfc = rfc,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };
        ctx.Clientes.Add(cliente);
        await ctx.SaveChangesAsync();
        return cliente.Id;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ValidData_ReturnsSuccessWithAuditFieldsSet()
    {
        var result = await _sut.CreateAsync(ValidCreateDto());

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.Nombre, Is.EqualTo("Cliente de Prueba"));

        await using var ctx = new ApplicationDbContext(_dbOptions);
        var entity = await ctx.Clientes.FirstAsync(c => c.Id == result.Value.Id);
        Assert.That(entity.CreatedBy, Is.EqualTo("test-user"));
        Assert.That(entity.ModifiedBy, Is.EqualTo("test-user"));
    }

    [Test]
    public async Task CreateAsync_DuplicateRfc_ReturnsFailure()
    {
        await SeedClienteAsync(rfc: "XAXX010101000");

        var result = await _sut.CreateAsync(ValidCreateDto(rfc: "XAXX010101000"));

        Assert.That(result.IsSuccess, Is.False, "Duplicate RFC must be rejected");

        await using var ctx = new ApplicationDbContext(_dbOptions);
        Assert.That(await ctx.Clientes.CountAsync(), Is.EqualTo(1),
            "No second client must be inserted when the RFC check fails");
    }

    [Test]
    public async Task CreateAsync_MultipleClientsWithoutRfc_AllSucceed()
    {
        // Rfc is optional — the uniqueness check must not trip on null/empty values.
        var first = await _sut.CreateAsync(ValidCreateDto());
        var second = await _sut.CreateAsync(ValidCreateDto());

        Assert.That(first.IsSuccess, Is.True, first.Error);
        Assert.That(second.IsSuccess, Is.True, second.Error);
    }

    [Test]
    public async Task CreateAsync_TieneDatosFiscalesTrueWithoutRazonSocial_ReturnsFailure()
    {
        var dto = ValidCreateDto();
        dto.TieneDatosFiscales = true;
        dto.RazonSocial = null;

        var result = await _sut.CreateAsync(dto);

        Assert.That(result.IsSuccess, Is.False,
            "Razón Social must be required when TieneDatosFiscales is true");

        await using var ctx = new ApplicationDbContext(_dbOptions);
        Assert.That(await ctx.Clientes.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateAsync_TieneDatosFiscalesTrueWithRazonSocial_Succeeds()
    {
        var dto = ValidCreateDto();
        dto.TieneDatosFiscales = true;
        dto.RazonSocial = "Cliente de Prueba S.A. de C.V.";

        var result = await _sut.CreateAsync(dto);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task CreateAsync_TieneDatosFiscalesFalse_RazonSocialNotRequired()
    {
        var dto = ValidCreateDto();
        dto.TieneDatosFiscales = false;
        dto.RazonSocial = null;

        var result = await _sut.CreateAsync(dto);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ClienteNotFound_ReturnsFailure()
    {
        var dto = new UpdateClienteDto { Nombre = "X", Pais = "Mexico" };

        var result = await _sut.UpdateAsync(999, dto);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task UpdateAsync_DuplicateRfcOnAnotherClient_ReturnsFailure()
    {
        await SeedClienteAsync("Otro Cliente", rfc: "XAXX010101000");
        var targetId = await SeedClienteAsync("Cliente a Editar", rfc: "AAA010101AAA");

        var dto = new UpdateClienteDto { Nombre = "Cliente a Editar", Pais = "Mexico", Rfc = "XAXX010101000" };

        var result = await _sut.UpdateAsync(targetId, dto);

        Assert.That(result.IsSuccess, Is.False, "RFC already used by a different client must be rejected");
    }

    [Test]
    public async Task UpdateAsync_SameRfcOnSameClient_Succeeds()
    {
        // Self-exclusion: saving the client with its own unchanged RFC must not trip the uniqueness check.
        var targetId = await SeedClienteAsync("Cliente a Editar", rfc: "AAA010101AAA");

        var dto = new UpdateClienteDto { Nombre = "Cliente a Editar Actualizado", Pais = "Mexico", Rfc = "AAA010101AAA" };

        var result = await _sut.UpdateAsync(targetId, dto);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task UpdateAsync_TieneDatosFiscalesTrueWithoutRazonSocial_ReturnsFailure()
    {
        var targetId = await SeedClienteAsync();

        var dto = new UpdateClienteDto
        {
            Nombre = "Cliente Existente",
            Pais = "Mexico",
            TieneDatosFiscales = true,
            RazonSocial = null
        };

        var result = await _sut.UpdateAsync(targetId, dto);

        Assert.That(result.IsSuccess, Is.False);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ClienteNotFound_ReturnsFailure()
    {
        var result = await _sut.DeleteAsync(999);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task DeleteAsync_ClienteWithExistingObras_ReturnsFailure()
    {
        var clienteId = await SeedClienteAsync();

        await using (var ctx = new ApplicationDbContext(_dbOptions))
        {
            ctx.Obras.Add(new Obra
            {
                ClienteId = clienteId,
                Direccion = "Calle Falsa 123",
                FechaSolicitud = DateTime.UtcNow,
                CreatedBy = "seed",
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        var result = await _sut.DeleteAsync(clienteId);

        Assert.That(result.IsSuccess, Is.False, "A client with existing Obras must not be deletable");

        await using var verifyCtx = new ApplicationDbContext(_dbOptions);
        Assert.That(await verifyCtx.Clientes.CountAsync(c => c.Id == clienteId), Is.EqualTo(1),
            "Client must remain when delete is blocked by existing Obras");
    }

    [Test]
    public async Task DeleteAsync_ClienteWithoutObras_Succeeds()
    {
        var clienteId = await SeedClienteAsync();

        var result = await _sut.DeleteAsync(clienteId);

        Assert.That(result.IsSuccess, Is.True, result.Error);

        await using var ctx = new ApplicationDbContext(_dbOptions);
        Assert.That(await ctx.Clientes.AnyAsync(c => c.Id == clienteId), Is.False,
            "Client must no longer be visible through the soft-delete filter");
    }

    // ── GetAllAsync / GetByIdAsync ────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ReturnsClientesOrderedByNombre()
    {
        await SeedClienteAsync("Zeta Servicios");
        await SeedClienteAsync("Alfa Mantenimiento");

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.Select(c => c.Nombre), Is.EqualTo(new[] { "Alfa Mantenimiento", "Zeta Servicios" }));
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task GetByIdAsync_Found_ReturnsMappedDto()
    {
        var clienteId = await SeedClienteAsync("Cliente Consultado", rfc: "BBB020202BBB");

        var result = await _sut.GetByIdAsync(clienteId);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value!.Nombre, Is.EqualTo("Cliente Consultado"));
        Assert.That(result.Value.Rfc, Is.EqualTo("BBB020202BBB"));
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
