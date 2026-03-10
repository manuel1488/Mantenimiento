using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Product;

public class CreateProductDto
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
    [Range(0.001, double.MaxValue)]
    public decimal Content { get; set; } = 1;

    public bool IsPartialSaleAllowed { get; set; } = false;

    public bool AllowCustomPricing { get; set; } = false;

    public bool RequiresInventory { get; set; } = true;

    [Required]
    public int UnitMeasureId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    public bool IsTaxable { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MexicoProductServiceId { get; set; }
}