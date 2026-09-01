using App.Core.Enums.Notifications;

namespace App.Core.Interfaces.Notifications;

/// <summary>
/// Fans an internal-alert business event out to every internal user subscribed to it, on every
/// channel they're subscribed on (currently only Telegram). Best-effort: never throws, mirroring
/// <see cref="INotificationService"/>'s own best-effort contract — callers should fire this after
/// their business transaction commits and never let it affect the caller's result.
/// </summary>
public interface IInternalNotificationDispatcher
{
    Task DispatchAsync(
        NotificationEventType eventType,
        string relatedEntityType,
        int relatedEntityId,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
