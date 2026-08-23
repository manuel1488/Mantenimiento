using App.Core.Enums.Shared;
using App.Models.Data.Contexts;
using App.Models.Shared;
using App.Services.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

namespace App.Services.Tests.Shared;

/// <summary>
/// Integration tests for AuditLogService backed by an EF Core in-memory database.
/// Covers the entityId filter used by per-record "Ver Historial" views (e.g. CotizacionHistorialDialog)
/// to scope the change log to a single record instead of the whole entity type.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AuditLogServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private AuditLogService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);
        _sut = new AuditLogService(_contextFactory, NullLogger<AuditLogService>.Instance);
    }

    private async Task SeedLogAsync(string entityType, string entityId, AuditAction action = AuditAction.Update)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.AuditLogs.Add(new AuditLog
        {
            EntityType = entityType,
            TableName = "cot_cotizaciones",
            EntityId = entityId,
            Action = action,
            UserName = "test-user",
            Timestamp = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    [Test]
    public async Task GetAsync_WithEntityId_OnlyReturnsMatchingRecord()
    {
        await SeedLogAsync("Cotizacion", "1");
        await SeedLogAsync("Cotizacion", "2");
        await SeedLogAsync("Cotizacion", "1");

        var (total, items) = await _sut.GetAsync(entityType: "Cotizacion", entityId: "1");

        Assert.That(total, Is.EqualTo(2));
        Assert.That(items.All(i => i.EntityId == "1"), Is.True);
    }

    [Test]
    public async Task GetAsync_WithoutEntityId_ReturnsAllRecordsOfType()
    {
        await SeedLogAsync("Cotizacion", "1");
        await SeedLogAsync("Cotizacion", "2");

        var (total, _) = await _sut.GetAsync(entityType: "Cotizacion");

        Assert.That(total, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAsync_EntityIdOfDifferentEntityType_IsNotConfused()
    {
        // A Cliente with EntityId "1" must not leak into a Cotizacion "1" history view.
        await SeedLogAsync("Cotizacion", "1");
        await SeedLogAsync("Cliente", "1");

        var (total, items) = await _sut.GetAsync(entityType: "Cotizacion", entityId: "1");

        Assert.That(total, Is.EqualTo(1));
        Assert.That(items.Single().EntityType, Is.EqualTo("Cotizacion"));
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
