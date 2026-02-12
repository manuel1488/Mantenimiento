using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for bulk updating product wholesale discount configurations.
/// </summary>
public class UpdateProductWholesalePricesDto
{
    [Required]
    public long ProductId { get; set; }

    /// <summary>
    /// List of wholesale discount configurations to set for the product.
    /// </summary>
    public List<CreateProductWholesalePriceDto> WholesalePrices { get; set; } = new();
}
