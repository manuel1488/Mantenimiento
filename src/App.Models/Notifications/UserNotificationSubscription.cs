using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Notifications;

namespace App.Models.Notifications;

/// <summary>Whether a given user wants a given <see cref="NotificationEventType"/> delivered on a given channel.</summary>
[Table("not_user_notification_subscriptions")]
public class UserNotificationSubscription : BaseEntity<int>
{
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = null!;

    [Required]
    public NotificationEventType EventType { get; set; }

    [Required]
    public NotificationChannelType ChannelType { get; set; }

    public bool Enabled { get; set; }
}
