using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;
using App.Models.Shared;

namespace App.Models.Shop;

[Table("sh_remissions")]
public class Remission : BaseEntity<long>
{
    [StringLength(20)]
    public string RemissionNumber { get; set; } = null!;

    public long CustomerId { get; set; }

    public DateTime RemissionDate { get; set; }

    public int LocationId { get; set; }

    public RemissionStatus Status { get; set; } = RemissionStatus.Pending;

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    // Consolidation tracking
    public long? ConsolidatedSaleId { get; set; }
    public DateTime? ConsolidatedAt { get; set; }

    [StringLength(200)]
    public string? ConsolidatedBy { get; set; }

    // Cancellation tracking
    [StringLength(500)]
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    // PDF snapshot
    public byte[]? PdfData { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey(nameof(ConsolidatedSaleId))]
    public virtual Sale? ConsolidatedSale { get; set; }

    public virtual ICollection<RemissionDetail> Details { get; set; } = new List<RemissionDetail>();
}
