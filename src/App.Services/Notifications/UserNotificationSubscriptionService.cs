using App.Core.Common;
using App.Core.DTOs.Notifications;
using App.Core.Enums.Notifications;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Models.Data.Contexts;
using App.Models.Notifications;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Notifications;

public class UserNotificationSubscriptionService : IUserNotificationSubscriptionService
{
    private static readonly NotificationEventType[] AllEventTypes = Enum.GetValues<NotificationEventType>();

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;
    private readonly IStringLocalizer<UserNotificationSubscriptionService> _localizer;
    private readonly ILogger<UserNotificationSubscriptionService> _logger;

    public UserNotificationSubscriptionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        IStringLocalizer<UserNotificationSubscriptionService> localizer,
        ILogger<UserNotificationSubscriptionService> logger)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Result<List<UserNotificationSubscriptionDto>>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await context.UserNotificationSubscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.ChannelType == NotificationChannelType.Telegram)
                .ToDictionaryAsync(s => s.EventType, cancellationToken);

            var result = AllEventTypes
                .Select(eventType => new UserNotificationSubscriptionDto
                {
                    EventType = eventType,
                    ChannelType = NotificationChannelType.Telegram,
                    Enabled = existing.TryGetValue(eventType, out var sub) && sub.Enabled
                })
                .ToList();

            return Result<List<UserNotificationSubscriptionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification subscriptions for user {UserId}", userId);
            return Result<List<UserNotificationSubscriptionDto>>.Failure(_localizer["Error getting notification subscriptions"]);
        }
    }

    public async Task<Result> UpdateAsync(string userId, List<UpdateUserNotificationSubscriptionDto> preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await context.UserNotificationSubscriptions
                .Where(s => s.UserId == userId)
                .ToDictionaryAsync(s => (s.EventType, s.ChannelType), cancellationToken);

            var now = _dateTimeService.Now;
            var currentUser = await _currentUserService.GetFullNameAsync() ?? "Unknown";

            foreach (var preference in preferences)
            {
                if (existing.TryGetValue((preference.EventType, preference.ChannelType), out var subscription))
                {
                    subscription.Enabled = preference.Enabled;
                    subscription.ModifiedBy = currentUser;
                    subscription.ModifiedAt = now;
                }
                else
                {
                    context.UserNotificationSubscriptions.Add(new UserNotificationSubscription
                    {
                        UserId = userId,
                        EventType = preference.EventType,
                        ChannelType = preference.ChannelType,
                        Enabled = preference.Enabled,
                        CreatedBy = currentUser,
                        CreatedAt = now
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification subscriptions for user {UserId}", userId);
            return Result.Failure(_localizer["Error updating notification subscriptions"]);
        }
    }

    public async Task<List<(string UserId, string ChatId)>> GetSubscribedTelegramChatIdsAsync(
        NotificationEventType eventType, NotificationChannelType channelType, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await (
            from subscription in context.UserNotificationSubscriptions
            join user in context.Users on subscription.UserId equals user.Id
            where subscription.EventType == eventType
                && subscription.ChannelType == channelType
                && subscription.Enabled
                && user.TelegramChatId != null
            select new { user.Id, user.TelegramChatId }
        ).ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.TelegramChatId!)).ToList();
    }
}
