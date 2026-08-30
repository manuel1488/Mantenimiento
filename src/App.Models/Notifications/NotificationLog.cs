using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Notifications;

namespace App.Models.Notifications;

/// <summary>Record of one delivery attempt of a <c>NotificationMessage</c> through one channel.</summary>
[Table("not_notificaciones_log")]
public class NotificationLog : BaseEntity<int>
{
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = null!;

    [Required]
    public NotificationChannelType Channel { get; set; }

    [Required]
    [StringLength(255)]
    public string RecipientAddress { get; set; } = null!;

    [Required]
    [StringLength(300)]
    public string Subject { get; set; } = null!;

    public bool Success { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    [StringLength(100)]
    public string? RelatedEntityType { get; set; }

    public int? RelatedEntityId { get; set; }

    [Required]
    public DateTime SentAt { get; set; }
}
