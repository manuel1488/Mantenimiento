using App.Core.Enums.Notifications;

namespace App.Core.Models.Notifications;

/// <summary>
/// Channel-agnostic notification payload. <see cref="Recipients"/> addresses the message per
/// channel (e.g. an email address for <see cref="NotificationChannelType.Email"/>, a chat id for
/// <see cref="NotificationChannelType.Telegram"/>) so that adding a new channel never requires
/// changing this class — only a new <c>INotificationChannel</c> implementation that reads its
/// own entry.
/// </summary>
public class NotificationMessage
{
    public string EventType { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public IReadOnlyDictionary<NotificationChannelType, string> Recipients { get; set; }
        = new Dictionary<NotificationChannelType, string>();
    public IReadOnlyList<NotificationAttachment> Attachments { get; set; }
        = new List<NotificationAttachment>();
}
