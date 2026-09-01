using App.Core.Enums.Notifications;

namespace App.Core.DTOs.Notifications;

public class UserNotificationSubscriptionDto
{
    public NotificationEventType EventType { get; set; }
    public NotificationChannelType ChannelType { get; set; }
    public bool Enabled { get; set; }
}

public class UpdateUserNotificationSubscriptionDto
{
    public NotificationEventType EventType { get; set; }
    public NotificationChannelType ChannelType { get; set; }
    public bool Enabled { get; set; }
}
