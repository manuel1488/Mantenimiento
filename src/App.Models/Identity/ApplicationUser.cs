using System.ComponentModel.DataAnnotations;
using App.Core.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace App.Models.Identity;

public class ApplicationUser : IdentityUser, IAuditableEntity, ISoftDelete
{
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }

    /// <summary>Telegram chat id linked via the PIN flow in the user's profile, used to deliver internal alerts.</summary>
    [StringLength(50)]
    public string? TelegramChatId { get; set; }

    // Implementación de IAuditableEntity
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Implementación de ISoftDelete
    public uint IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}