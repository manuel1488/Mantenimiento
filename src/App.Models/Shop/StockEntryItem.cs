using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_stock_entry_items")]
public class StockEntryItem : BaseEntity<long>
{
    public long StockEntryId { get; set; }

    public long ProductId { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? UnitCost { get; set; }

    public long? InventoryMovementId { get; set; }

    [ForeignKey(nameof(StockEntryId))]
    public virtual StockEntry StockEntry { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(InventoryMovementId))]
    public virtual InventoryMovement? InventoryMovement { get; set; }
}
