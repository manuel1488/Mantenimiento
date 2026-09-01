using App.Core.Common;
using App.Core.DTOs.Notifications;
using App.Core.Enums.Notifications;

namespace App.Core.Interfaces.Notifications;

public interface IUserNotificationSubscriptionService
{
    /// <summary>Returns the user's preference for every <see cref="NotificationEventType"/> on the
    /// Telegram channel, defaulting to disabled for any event without a stored row yet.</summary>
    Task<Result<List<UserNotificationSubscriptionDto>>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(string userId, List<UpdateUserNotificationSubscriptionDto> preferences, CancellationToken cancellationToken = default);

    /// <summary>Chat ids of every user subscribed to <paramref name="eventType"/> on <paramref name="channelType"/>
    /// that also has a non-null recipient address for that channel (e.g. TelegramChatId).</summary>
    Task<List<(string UserId, string ChatId)>> GetSubscribedTelegramChatIdsAsync(
        NotificationEventType eventType, NotificationChannelType channelType, CancellationToken cancellationToken = default);
}
