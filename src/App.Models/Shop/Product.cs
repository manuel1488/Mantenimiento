using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Models.Billing;

namespace App.Models.Shop;

[Table("sh_products")]
public class Product : BaseEntity<long>
{
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = null!;

    [StringLength(50)]
    public string? Barcode { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = null!;

    public int UnitMeasureId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Cost { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public bool IsTaxable { get; set; }

    public bool IsActive { get; set; }

    public int? MexicoProductServiceId { get; set; }

    public decimal Content { get; set; } = 1;

    public bool IsPartialSaleAllowed { get; set; } = false;

    public bool AllowCustomPricing { get; set; } = false;

    [ForeignKey(nameof(UnitMeasureId))]
    public virtual UnitMeasure UnitMeasure { get; set; } = null!;

    [ForeignKey(nameof(MexicoProductServiceId))]
    public virtual MexicoProductService? MexicoProductService { get; set; }

    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductPartialSurcharge> PartialSurcharges { get; set; } = new List<ProductPartialSurcharge>();

    public virtual ICollection<ProductWholesalePrice> WholesalePrices { get; set; } = new List<ProductWholesalePrice>();
}