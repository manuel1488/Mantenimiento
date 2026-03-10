using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;
using App.Models.Shared;

namespace App.Models.Shop;

[Table("sh_quotations")]
public class Quotation : BaseEntity<long>
{
    [StringLength(20)]
    public string QuotationNumber { get; set; } = null!;

    public long CustomerId { get; set; }

    public DateTime QuoteDate { get; set; }

    public DateTime ValidUntil { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public DateTime? SentAt { get; set; }

    [StringLength(200)]
    public string? SentToEmail { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<QuotationDetail> Details { get; set; } = new List<QuotationDetail>();
}
