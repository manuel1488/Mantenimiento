using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Product;

public class UpdateProductDto
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

    [Required]
    public int UnitMeasureId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Content { get; set; }

    public bool IsPartialSaleAllowed { get; set; }

    [Range(0.000001, double.MaxValue)]
    public decimal QuantityStep { get; set; } = 1;

    public bool IsLabelingAllowed { get; set; }

    public bool AllowCustomPricing { get; set; }

    public bool RequiresInventory { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    [Required]
    [Range(0.000001, double.MaxValue)]
    public decimal Price { get; set; }

    public bool IsTaxable { get; set; }
    public bool IsActive { get; set; }

    public int? MexicoProductServiceId { get; set; }
}