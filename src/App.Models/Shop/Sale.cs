using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Constants;
using App.Core.Enums.Shop;
using App.Models.Shared;

namespace App.Models.Shop;

[Table("sh_sales")]
public class Sale : BaseEntity<long>
{
    public long CustomerId { get; set; }
    public DateTime SaleDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    // Discount fields
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// Rounding adjustment amount (positive = rounded up, negative = rounded down)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal RoundingAmount { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public App.Core.Enums.Shop.SaleStatus Status { get; set; }

    public SaleType SaleType { get; set; } = SaleType.Public;

    [StringLength(200)]
    public string? DiscountAuthorizedBy { get; set; }

    public string? DiscountAuthorizerId { get; set; }
    public DateTime? DiscountAuthorizedAt { get; set; }

    // Location relationship (optional - nullable for pre-location sales)
    public int? LocationId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }

    // Cash register relationship (nullable for backward compatibility with existing sales)
    public long? CashRegisterId { get; set; }

    [ForeignKey(nameof(CashRegisterId))]
    public virtual CashRegister? CashRegister { get; set; }

    // Quotation origin (optional)
    public long? QuotationId { get; set; }

    [ForeignKey(nameof(QuotationId))]
    public virtual Quotation? Quotation { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public virtual ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();

    public virtual ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
}