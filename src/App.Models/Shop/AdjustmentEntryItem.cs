using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_adjustment_entry_items")]
public class AdjustmentEntryItem : BaseEntity<long>
{
    public long AdjustmentEntryId { get; set; }

    public long ProductId { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal NewQuantity { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal PreviousQuantity { get; set; }

    public long? InventoryMovementId { get; set; }

    [ForeignKey(nameof(AdjustmentEntryId))]
    public virtual AdjustmentEntry AdjustmentEntry { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(InventoryMovementId))]
    public virtual InventoryMovement? InventoryMovement { get; set; }
}
