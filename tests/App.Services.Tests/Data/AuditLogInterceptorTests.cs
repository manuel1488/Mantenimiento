using System.Text.Json;

using App.Core.Enums.Shared;
using App.Models.Data.Contexts;
using App.Models.Data.Interceptors;
using App.Models.Settings;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using NUnit.Framework;

namespace App.Services.Tests.Data;

/// <summary>
/// Integration tests (EF Core InMemory) for <see cref="AuditLogInterceptor"/>.
/// Verifies that change history is captured for IAuditTracked entities, that insert
/// primary keys are resolved after save, that sensitive properties are redacted, and
/// that soft-deletes (Modified + IsDeleted bump, as left by AuditableEntityInterceptor)
/// are classified as Delete.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AuditLogInterceptorTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private static readonly DateTime _fixedNow = new(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc);

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    [SetUp]
    public void Setup()
    {
        var dateTime = new Mock<IDateTime>();
        dateTime.SetupGet(d => d.Now).Returns(_fixedNow);

        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new AuditLogInterceptor(dateTime.Object))
            .Options;
    }

    private ApplicationDbContext NewContext() => new(_dbOptions);

    private static Product NewProduct() => new()
    {
        Code = "P-001",
        Name = "Test Product",
        Brand = "ACME",
        UnitMeasureId = 1,
        Cost = 5m,
        Price = 10m,
        IsActive = true,
        CreatedBy = "alice",
        CreatedAt = _fixedNow
    };

    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Insert_TrackedEntity_WritesInsertLogWithResolvedKey()
    {
        long productId;
        await using (var ctx = NewContext())
        {
            var product = NewProduct();
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            productId = product.Id;
        }

        await using var verify = NewContext();
        var logs = await verify.AuditLogs.ToListAsync();

        Assert.That(logs, Has.Count.EqualTo(1));
        Assert.That(logs[0].Action, Is.EqualTo(AuditAction.Insert));
        Assert.That(logs[0].EntityType, Is.EqualTo(nameof(Product)));
        // InMemory has no relational table mapping, so GetTableName() falls back to the
        // CLR name; on MySQL this resolves to "sh_products".
        Assert.That(logs[0].TableName, Is.Not.Empty);
        Assert.That(logs[0].EntityId, Is.EqualTo(productId.ToString()));
        Assert.That(logs[0].UserName, Is.EqualTo("alice"));
        Assert.That(logs[0].Timestamp, Is.EqualTo(_fixedNow));
    }

    [Test]
    public async Task Update_TrackedEntity_RecordsOnlyChangedPropertiesAsOldNew()
    {
        long productId;
        await using (var ctx = NewContext())
        {
            var product = NewProduct();
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            productId = product.Id;
        }

        await using (var ctx = NewContext())
        {
            var product = await ctx.Products.FirstAsync(p => p.Id == productId);
            product.Price = 12.5m;
            product.ModifiedBy = "bob";
            product.ModifiedAt = _fixedNow;
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var update = await verify.AuditLogs.SingleAsync(l => l.Action == AuditAction.Update);

        Assert.That(update.UserName, Is.EqualTo("bob"));

        var changes = JsonSerializer.Deserialize<List<PropertyChangeDto>>(update.Changes!)!;
        Assert.That(changes, Has.Count.EqualTo(1), "Only the changed Price should be recorded");
        Assert.That(changes[0].Property, Is.EqualTo(nameof(Product.Price)));
        Assert.That(changes[0].Old, Is.EqualTo("10"));
        Assert.That(changes[0].New, Is.EqualTo("12.5"));
    }

    [Test]
    public async Task Update_NoMeaningfulChange_DoesNotWriteLog()
    {
        long productId;
        await using (var ctx = NewContext())
        {
            var product = NewProduct();
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            productId = product.Id;
        }

        await using (var ctx = NewContext())
        {
            var product = await ctx.Products.FirstAsync(p => p.Id == productId);
            // Only audit-metadata changes — no business property changed.
            product.ModifiedBy = "bob";
            product.ModifiedAt = _fixedNow;
            ctx.Entry(product).Property(p => p.Price).IsModified = true; // same value
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var updates = await verify.AuditLogs.CountAsync(l => l.Action == AuditAction.Update);
        Assert.That(updates, Is.EqualTo(0));
    }

    [Test]
    public async Task Update_SensitiveProperty_RedactsValueButRecordsChange()
    {
        await using (var ctx = NewContext())
        {
            ctx.EmailSettings.Add(new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.old.com",
                SmtpPassword = "OldSecret123",
                CreatedBy = "alice",
                CreatedAt = _fixedNow
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var settings = await ctx.EmailSettings.FirstAsync();
            settings.SmtpPassword = "BrandNewSecret456";
            settings.ModifiedBy = "bob";
            settings.ModifiedAt = _fixedNow;
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var update = await verify.AuditLogs.SingleAsync(l => l.Action == AuditAction.Update);

        Assert.That(update.Changes, Does.Not.Contain("OldSecret123"));
        Assert.That(update.Changes, Does.Not.Contain("BrandNewSecret456"));

        var changes = JsonSerializer.Deserialize<List<PropertyChangeDto>>(update.Changes!)!;
        var pwd = changes.Single(c => c.Property == nameof(EmailSettings.SmtpPassword));
        Assert.That(pwd.Old, Is.EqualTo("********"));
        Assert.That(pwd.New, Is.EqualTo("********"));
    }

    [Test]
    public async Task SoftDelete_ClassifiedAsDelete()
    {
        long productId;
        await using (var ctx = NewContext())
        {
            var product = NewProduct();
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            productId = product.Id;
        }

        await using (var ctx = NewContext())
        {
            var product = await ctx.Products.FirstAsync(p => p.Id == productId);
            // Emulate what AuditableEntityInterceptor leaves for a soft-delete:
            // state Modified, IsDeleted bumped 0 -> N, DeletedBy set.
            product.IsDeleted = 1;
            product.DeletedBy = "carol";
            product.DeletedAt = _fixedNow;
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var del = await verify.AuditLogs.SingleAsync(l => l.Action == AuditAction.Delete);

        Assert.That(del.EntityId, Is.EqualTo(productId.ToString()));
        Assert.That(del.UserName, Is.EqualTo("carol"));
        Assert.That(del.Changes, Is.Null, "Delete records no property diff");
    }

    [Test]
    public async Task NonTrackedEntity_IsNotAudited()
    {
        await using (var ctx = NewContext())
        {
            ctx.Sales.Add(new Sale
            {
                CustomerId = 1,
                SaleDate = _fixedNow,
                Total = 100m,
                CreatedBy = "alice",
                CreatedAt = _fixedNow
            });
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        Assert.That(await verify.AuditLogs.AnyAsync(), Is.False);
    }

    private class PropertyChangeDto
    {
        public string Property { get; set; } = null!;
        public string? Old { get; set; }
        public string? New { get; set; }
    }
}
