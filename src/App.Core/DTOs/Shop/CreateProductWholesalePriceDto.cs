using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for creating/updating a product wholesale discount configuration.
/// </summary>
public class CreateProductWholesalePriceDto
{
    [Required]
    public int WholesaleTierId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal MinQuantity { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
    public decimal DiscountPercentage { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? FixedPrice { get; set; }

    public bool IsActive { get; set; } = true;
}
