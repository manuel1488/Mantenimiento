using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_adjustment_entries")]
public class AdjustmentEntry : BaseEntity<long>
{
    [Required]
    [StringLength(20)]
    public string AdjustmentType { get; set; } = null!;

    public int LocationId { get; set; }

    [StringLength(50)]
    public string? Reference { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = null!;

    public DateTime AdjustmentDate { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<AdjustmentEntryItem> Items { get; set; } = [];
}
