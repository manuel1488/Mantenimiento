using App.Core.Common;
using App.Core.DTOs.Notifications;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Models.Data.Contexts;
using App.Models.Notifications;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Notifications;

/// <summary>
/// Links a Telegram chat to an ApplicationUser via a 6-digit PIN. <see cref="TryLinkAsync"/> runs
/// from the Telegram webhook, which is unauthenticated — there is no ASP.NET user to stamp as
/// ModifiedBy, so it stamps a synthetic "Telegram Bot" identity instead of using
/// <see cref="ICurrentUserService"/>.
/// </summary>
public class TelegramLinkService : ITelegramLinkService
{
    private const int CodeExpirationMinutes = 10;
    private const string TelegramBotIdentity = "Telegram Bot";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;
    private readonly ITelegramSettingsService _telegramSettingsService;
    private readonly IStringLocalizer<TelegramLinkService> _localizer;
    private readonly ILogger<TelegramLinkService> _logger;

    public TelegramLinkService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        ITelegramSettingsService telegramSettingsService,
        IStringLocalizer<TelegramLinkService> localizer,
        ILogger<TelegramLinkService> logger)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _telegramSettingsService = telegramSettingsService;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Result<TelegramLinkCodeDto>> GenerateLinkCodeAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var pendingCodes = await context.UserTelegramLinkCodes
                .Where(c => c.UserId == userId && !c.Used)
                .ToListAsync(cancellationToken);
            foreach (var pending in pendingCodes)
                pending.Used = true;

            var now = _dateTimeService.Now;
            var code = new UserTelegramLinkCode
            {
                UserId = userId,
                Code = Random.Shared.Next(100000, 1000000).ToString(),
                ExpiresAt = now.AddMinutes(CodeExpirationMinutes),
                Used = false,
                CreatedAt = now
            };
            context.UserTelegramLinkCodes.Add(code);
            await context.SaveChangesAsync(cancellationToken);

            var settings = await _telegramSettingsService.GetSettingsAsync();

            return Result<TelegramLinkCodeDto>.Success(new TelegramLinkCodeDto
            {
                Code = code.Code,
                ExpiresAt = code.ExpiresAt,
                BotUsername = settings?.BotUsername
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Telegram link code for user {UserId}", userId);
            return Result<TelegramLinkCodeDto>.Failure(_localizer["Error generating link code"]);
        }
    }

    public async Task<Result<string>> TryLinkAsync(string code, string chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var now = _dateTimeService.Now;
            var linkCode = await context.UserTelegramLinkCodes
                .FirstOrDefaultAsync(c => c.Code == code && !c.Used && c.ExpiresAt > now, cancellationToken);

            if (linkCode == null)
                return Result<string>.Failure(_localizer["Invalid or expired code"]);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == linkCode.UserId, cancellationToken);
            if (user == null)
                return Result<string>.Failure(_localizer["User not found"]);

            user.TelegramChatId = chatId;
            user.ModifiedBy = TelegramBotIdentity;
            user.ModifiedAt = now;

            linkCode.Used = true;

            await context.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking Telegram code");
            return Result<string>.Failure(_localizer["Error linking Telegram account"]);
        }
    }

    public async Task<bool> IsLinkedAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TelegramChatId)
            .FirstOrDefaultAsync(cancellationToken) is { Length: > 0 };
    }

    public async Task<Result> UnlinkAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return Result.Failure(_localizer["User not found"]);

            user.TelegramChatId = null;
            user.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            user.ModifiedAt = _dateTimeService.Now;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking Telegram account for user {UserId}", userId);
            return Result.Failure(_localizer["Error unlinking Telegram account"]);
        }
    }
}
