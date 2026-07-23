using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;


[Table("sh_inventory_movements")]
public class InventoryMovement : BaseEntity<long>
{
    public long ProductId { get; set; }
    public int LocationId { get; set; }
    public int? DestinationLocationId { get; set; }

    [Required]
    [StringLength(20)]
    public string MovementType { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string MovementSubType { get; set; } = null!;

    /// <summary>
    /// Container units moved (e.g., 0.5 containers or full containers)
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Individual units moved (e.g., 9.5 liters)
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal IndividualUnits { get; set; }

    [StringLength(50)]
    public string? Reference { get; set; }

    [StringLength(50)]
    public string? Document { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Previous balance in container units (Quantity)
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal PreviousBalance { get; set; }

    /// <summary>
    /// New balance in container units (Quantity)
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal NewBalance { get; set; }

    /// <summary>
    /// Previous balance in individual units
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal PreviousIndividualBalance { get; set; }

    /// <summary>
    /// New balance in individual units
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal NewIndividualBalance { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? UnitCost { get; set; }

    public DateTime MovementDate { get; set; }

    [StringLength(100)]
    public string? RelatedParty { get; set; }

    public long? SupplierId { get; set; }

    public long? StockEntryId { get; set; }

    public long? AdjustmentEntryId { get; set; }

    /// <summary>
    /// Groups movements created together in a single bulk transfer operation
    /// </summary>
    public Guid? BatchId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey(nameof(DestinationLocationId))]
    public virtual Location? DestinationLocation { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(StockEntryId))]
    public virtual StockEntry? StockEntry { get; set; }

    [ForeignKey(nameof(AdjustmentEntryId))]
    public virtual AdjustmentEntry? AdjustmentEntry { get; set; }
}