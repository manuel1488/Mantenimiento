using App.Core.Common;
using App.Core.Models.Notifications;

namespace App.Core.Interfaces.Notifications;

/// <summary>
/// Fans a <see cref="NotificationMessage"/> out to every registered <see cref="INotificationChannel"/>
/// that can handle it, logging each attempt. Delivery is best-effort: a failing channel never
/// throws, so callers should not use the returned <see cref="Result"/> to roll back business state.
/// </summary>
public interface INotificationService
{
    Task<Result> NotifyAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
