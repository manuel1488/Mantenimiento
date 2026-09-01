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
/// Integration tests for TelegramLinkService backed by an EF Core in-memory database. Covers the
/// PIN-based account linking flow: generate, consume (success/failure paths), and unlink.
/// </summary>
[TestFixture]
[Category("Integration")]
public class TelegramLinkServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
    private TestDbContextFactory _contextFactory = null!;
    private Mock<IDateTime> _dateTimeMock = null!;
    private Mock<ICurrentUserService> _currentUserMock = null!;
    private Mock<ITelegramSettingsService> _telegramSettingsMock = null!;
    private TelegramLinkService _sut = null!;
    private const string UserId = "user-1";

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

        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(u => u.GetFullNameAsync()).ReturnsAsync("Test Admin");

        _telegramSettingsMock = new Mock<ITelegramSettingsService>();
        _telegramSettingsMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync((App.Core.DTOs.Settings.TelegramSettingsDto?)null);

        var localizerMock = new Mock<IStringLocalizer<TelegramLinkService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        _sut = new TelegramLinkService(
            _contextFactory,
            _currentUserMock.Object,
            _dateTimeMock.Object,
            _telegramSettingsMock.Object,
            localizerMock.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TelegramLinkService>.Instance);

        SeedUserAsync().GetAwaiter().GetResult();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void SetNow(DateTime utcNow) => _dateTimeMock.Setup(d => d.Now).Returns(utcNow);

    private async Task SeedUserAsync()
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        ctx.Users.Add(new ApplicationUser
        {
            Id = UserId,
            UserName = "tester",
            FullName = "Test User",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
    }

    // ── GenerateLinkCodeAsync ───────────────────────────────────────────────

    [Test]
    public async Task GenerateLinkCodeAsync_ReturnsSixDigitCode()
    {
        var result = await _sut.GenerateLinkCodeAsync(UserId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Code, Has.Length.EqualTo(6));
        Assert.That(int.TryParse(result.Value.Code, out _), Is.True);
    }

    [Test]
    public async Task GenerateLinkCodeAsync_SetsExpiryTenMinutesOut()
    {
        var result = await _sut.GenerateLinkCodeAsync(UserId);

        Assert.That(result.Value!.ExpiresAt, Is.EqualTo(new DateTime(2026, 6, 15, 12, 10, 0, DateTimeKind.Utc)));
    }

    [Test]
    public async Task GenerateLinkCodeAsync_PreviousUnusedCode_IsInvalidated()
    {
        var first = await _sut.GenerateLinkCodeAsync(UserId);
        await _sut.GenerateLinkCodeAsync(UserId);

        var linkResult = await _sut.TryLinkAsync(first.Value!.Code, "chat-1");

        Assert.That(linkResult.IsSuccess, Is.False, "The first code must no longer be usable once a second one is generated");
    }

    // ── TryLinkAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task TryLinkAsync_ValidCode_LinksChatIdToUser()
    {
        var code = (await _sut.GenerateLinkCodeAsync(UserId)).Value!.Code;

        var result = await _sut.TryLinkAsync(code, "chat-123");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(UserId));
        Assert.That(await _sut.IsLinkedAsync(UserId), Is.True);
    }

    [Test]
    public async Task TryLinkAsync_UnknownCode_Fails()
    {
        var result = await _sut.TryLinkAsync("000000", "chat-123");

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task TryLinkAsync_ExpiredCode_Fails()
    {
        var code = (await _sut.GenerateLinkCodeAsync(UserId)).Value!.Code;

        SetNow(new DateTime(2026, 6, 15, 12, 11, 0, DateTimeKind.Utc)); // 11 min later, past the 10-min expiry
        var result = await _sut.TryLinkAsync(code, "chat-123");

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task TryLinkAsync_CodeAlreadyUsed_CannotBeReused()
    {
        var code = (await _sut.GenerateLinkCodeAsync(UserId)).Value!.Code;
        await _sut.TryLinkAsync(code, "chat-123");

        var secondAttempt = await _sut.TryLinkAsync(code, "chat-456");

        Assert.That(secondAttempt.IsSuccess, Is.False);
    }

    // ── UnlinkAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task UnlinkAsync_ClearsChatId()
    {
        var code = (await _sut.GenerateLinkCodeAsync(UserId)).Value!.Code;
        await _sut.TryLinkAsync(code, "chat-123");

        var result = await _sut.UnlinkAsync(UserId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await _sut.IsLinkedAsync(UserId), Is.False);
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
