using App.Core.Common;
using App.Core.Enums.Notifications;
using App.Core.Models.Notifications;

namespace App.Core.Interfaces.Notifications;

/// <summary>
/// A single delivery strategy (email, Telegram, WhatsApp, ...). New channels are added by
/// implementing this interface and registering it in DI — no changes to
/// <see cref="INotificationService"/> or its callers are needed.
/// </summary>
public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }

    /// <summary>Whether this channel has the data it needs to deliver the given message.</summary>
    bool CanSend(NotificationMessage message);

    Task<Result> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
