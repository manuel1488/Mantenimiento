using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_sale_details")]
public class SaleDetail : BaseEntity<long>
{
    public long SaleId { get; set; }

    public long ProductId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public bool IsDiscountAuthorized { get; set; } = false;

    public bool IsCustomPrice { get; set; } = false;

    [StringLength(200)]
    public string? DiscountAuthorizedBy { get; set; }

    public string? DiscountAuthorizerId { get; set; }
    
    public DateTime? DiscountAuthorizedAt { get; set; }

    /// <summary>
    /// Reference to the partial sale fraction used (if applicable).
    /// </summary>
    public int? PartialSaleFractionId { get; set; }

    /// <summary>
    /// Surcharge percentage applied for partial sale.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal SurchargePercentage { get; set; } = 0;

    /// <summary>
    /// Surcharge amount in currency.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal SurchargeAmount { get; set; } = 0;

    /// <summary>
    /// Base price before surcharge was applied.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePriceBeforeSurcharge { get; set; } = 0;

    [ForeignKey(nameof(SaleId))]
    public virtual Sale Sale { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(PartialSaleFractionId))]
    public virtual PartialSaleFraction? PartialSaleFraction { get; set; }
}