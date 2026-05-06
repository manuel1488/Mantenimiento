using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;


[Table("sh_inventory")]
public class Inventory : BaseEntity<long>
{
    public long ProductId { get; set; }

    /// <summary>
    /// Storage location ID (Warehouse or Branch)
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// Container/Package units (e.g., 100 containers or 1.5 containers for partial)
    /// </summary>
    [Column(TypeName = "decimal(15,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal? MinStock { get; set; }

    [Column(TypeName = "decimal(15,6)")]
    public decimal? MaxStock { get; set; }

    [Timestamp]
    public byte[] Version { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;
}