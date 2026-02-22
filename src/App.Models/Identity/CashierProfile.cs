using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Interfaces;
using App.Models.Shop;

namespace App.Models.Identity;

[Table("id_cashier_profiles")]
public class CashierProfile : IAuditableEntity
{
    public long Id { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = null!;

    public int LocationId { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    // IAuditableEntity
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
