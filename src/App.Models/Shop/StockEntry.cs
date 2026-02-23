using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_stock_entries")]
public class StockEntry : BaseEntity<long>
{
    [Required]
    [StringLength(20)]
    public string MovementType { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string MovementSubType { get; set; } = null!;

    public int LocationId { get; set; }

    public long? SupplierId { get; set; }

    [StringLength(100)]
    public string? SupplierName { get; set; }

    [StringLength(100)]
    public string? DocumentNumber { get; set; }

    [StringLength(50)]
    public string? Reference { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = null!;

    public DateTime EntryDate { get; set; }

    [StringLength(255)]
    public string? AttachmentFileName { get; set; }

    [StringLength(100)]
    public string? AttachmentMimeType { get; set; }

    public byte[]? AttachmentData { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<StockEntryItem> Items { get; set; } = [];
}
