using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_physical_inventory_count_lines")]
public class PhysicalInventoryCountLine : BaseEntity<long>
{
    public long PhysicalInventoryCountId { get; set; }

    public long ProductId { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal SystemQuantity { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal CountedQuantity { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal Difference { get; set; }

    /// <summary>
    /// Null when Difference == 0 - no inventory movement is generated for lines with no discrepancy
    /// </summary>
    public long? InventoryMovementId { get; set; }

    [ForeignKey(nameof(PhysicalInventoryCountId))]
    public virtual PhysicalInventoryCount PhysicalInventoryCount { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(InventoryMovementId))]
    public virtual InventoryMovement? InventoryMovement { get; set; }
}
