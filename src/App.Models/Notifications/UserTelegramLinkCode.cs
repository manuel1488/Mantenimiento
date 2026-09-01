using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Interfaces;

namespace App.Models.Notifications;

/// <summary>
/// Short-lived PIN a user generates in their profile and sends to the Telegram bot to link their
/// account. Not an <see cref="IAuditableEntity"/>: it is consumed from the Telegram webhook, which
/// runs unauthenticated (no ASP.NET user), so there is no "current user" to stamp as ModifiedBy.
/// </summary>
[Table("not_telegram_link_codes")]
public class UserTelegramLinkCode : IEntity<int>
{
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = null!;

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }

    public DateTime CreatedAt { get; set; }
}
