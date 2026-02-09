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

    [Required]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = null!;

    public SaleType SaleType { get; set; } = SaleType.Public;

    [StringLength(200)]
    public string? DiscountAuthorizedBy { get; set; }

    public string? DiscountAuthorizerId { get; set; }
    public DateTime? DiscountAuthorizedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}