using App.Core.Enums.Notifications;
using App.Core.DTOs.Notifications;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Services.Notifications;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;

using Moq;

using NUnit.Framework;

namespace App.Services.Tests.Notifications;

/// <summary>
/// Integration tests for UserNotificationSubscriptionService backed by an EF Core in-memory
/// database. Covers the default-off catalog for users with no stored preference, the upsert
/// semantics of UpdateAsync, and the join used to resolve who gets an internal alert.
/// </summary>
[TestFixture]
[Category("Integration")]
public class UserNotificationSubscriptionServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private UserNotificationSubscriptionService _sut = null!;
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    [SetUp]
    public void SetUp()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _contextFactory = new TestDbContextFactory(_dbOptions);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test Admin");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var localizerMock = new Mock<IStringLocalizer<UserNotificationSubscriptionService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        _sut = new UserNotificationSubscriptionService(
            _contextFactory,
            currentUserMock.Object,
            dateTimeMock.Object,
            localizerMock.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UserNotificationSubscriptionService>.Instance);
    }

    private async Task SeedUserAsync(string userId, string? telegramChatId)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            FullName = userId,
            TelegramChatId = telegramChatId,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    // ── GetForUserAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetForUserAsync_NoStoredRows_ReturnsAllEventsDisabled()
    {
        var result = await _sut.GetForUserAsync(UserId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(Enum.GetValues<NotificationEventType>().Length));
        Assert.That(result.Value!.All(p => !p.Enabled), Is.True);
    }

    [Test]
    public async Task GetForUserAsync_AfterEnablingOne_ReflectsStoredValue()
    {
        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = true }
        });

        var result = await _sut.GetForUserAsync(UserId);

        var obraIniciada = result.Value!.Single(p => p.EventType == NotificationEventType.ObraIniciada);
        Assert.That(obraIniciada.Enabled, Is.True);
        Assert.That(result.Value!.Where(p => p.EventType != NotificationEventType.ObraIniciada).All(p => !p.Enabled), Is.True);
    }

    // ── UpdateAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_TogglingOff_PersistsDisabled()
    {
        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = true }
        });

        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = false }
        });

        var result = await _sut.GetForUserAsync(UserId);
        Assert.That(result.Value!.Single(p => p.EventType == NotificationEventType.ObraIniciada).Enabled, Is.False);
    }

    // ── GetSubscribedTelegramChatIdsAsync ────────────────────────────────────

    [Test]
    public async Task GetSubscribedTelegramChatIdsAsync_SubscribedWithChatId_IsIncluded()
    {
        await SeedUserAsync(UserId, "chat-123");
        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = true }
        });

        var subscribers = await _sut.GetSubscribedTelegramChatIdsAsync(NotificationEventType.ObraIniciada, NotificationChannelType.Telegram);

        Assert.That(subscribers, Has.Count.EqualTo(1));
        Assert.That(subscribers[0], Is.EqualTo((UserId, "chat-123")));
    }

    [Test]
    public async Task GetSubscribedTelegramChatIdsAsync_SubscribedButNoChatId_IsExcluded()
    {
        await SeedUserAsync(UserId, telegramChatId: null);
        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = true }
        });

        var subscribers = await _sut.GetSubscribedTelegramChatIdsAsync(NotificationEventType.ObraIniciada, NotificationChannelType.Telegram);

        Assert.That(subscribers, Is.Empty, "A user without a linked Telegram chat must never receive a dispatch");
    }

    [Test]
    public async Task GetSubscribedTelegramChatIdsAsync_HasChatIdButNotSubscribed_IsExcluded()
    {
        await SeedUserAsync(UserId, "chat-123");
        // No subscription created — default is disabled.

        var subscribers = await _sut.GetSubscribedTelegramChatIdsAsync(NotificationEventType.ObraIniciada, NotificationChannelType.Telegram);

        Assert.That(subscribers, Is.Empty);
    }

    [Test]
    public async Task GetSubscribedTelegramChatIdsAsync_OtherEventType_IsExcluded()
    {
        await SeedUserAsync(UserId, "chat-123");
        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.CotizacionAprobada, ChannelType = NotificationChannelType.Telegram, Enabled = true }
        });

        var subscribers = await _sut.GetSubscribedTelegramChatIdsAsync(NotificationEventType.ObraIniciada, NotificationChannelType.Telegram);

        Assert.That(subscribers, Is.Empty);
    }

    [Test]
    public async Task GetSubscribedTelegramChatIdsAsync_MultipleUsers_ReturnsOnlyMatching()
    {
        await SeedUserAsync(UserId, "chat-123");
        await SeedUserAsync(OtherUserId, "chat-456");

        await _sut.UpdateAsync(UserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = true }
        });
        await _sut.UpdateAsync(OtherUserId, new List<UpdateUserNotificationSubscriptionDto>
        {
            new() { EventType = NotificationEventType.ObraIniciada, ChannelType = NotificationChannelType.Telegram, Enabled = false }
        });

        var subscribers = await _sut.GetSubscribedTelegramChatIdsAsync(NotificationEventType.ObraIniciada, NotificationChannelType.Telegram);

        Assert.That(subscribers, Has.Count.EqualTo(1));
        Assert.That(subscribers[0].UserId, Is.EqualTo(UserId));
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
