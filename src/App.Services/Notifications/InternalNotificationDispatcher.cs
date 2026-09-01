using App.Core.Enums.Notifications;
using App.Core.Interfaces.Notifications;
using App.Core.Models.Notifications;
using Microsoft.Extensions.Logging;

namespace App.Services.Notifications;

/// <summary>
/// Resolves which internal users are subscribed to a business event and fans it out to them, one
/// <see cref="NotificationMessage"/> per user, via the existing <see cref="INotificationService"/>
/// fan-out/log (ADR-011) — this class only adds the "who's subscribed" resolution on top of it.
/// </summary>
public class InternalNotificationDispatcher : IInternalNotificationDispatcher
{
    private readonly IUserNotificationSubscriptionService _subscriptionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<InternalNotificationDispatcher> _logger;

    public InternalNotificationDispatcher(
        IUserNotificationSubscriptionService subscriptionService,
        INotificationService notificationService,
        ILogger<InternalNotificationDispatcher> logger)
    {
        _subscriptionService = subscriptionService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task DispatchAsync(
        NotificationEventType eventType,
        string relatedEntityType,
        int relatedEntityId,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscribers = await _subscriptionService.GetSubscribedTelegramChatIdsAsync(
                eventType, NotificationChannelType.Telegram, cancellationToken);

            foreach (var (userId, chatId) in subscribers)
            {
                var message = new NotificationMessage
                {
                    EventType = eventType.ToString(),
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    Subject = subject,
                    Body = body,
                    Recipients = new Dictionary<NotificationChannelType, string>
                    {
                        [NotificationChannelType.Telegram] = chatId
                    }
                };

                await _notificationService.NotifyAsync(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching internal alert {EventType} for {RelatedEntityType} {RelatedEntityId}",
                eventType, relatedEntityType, relatedEntityId);
        }
    }
}
