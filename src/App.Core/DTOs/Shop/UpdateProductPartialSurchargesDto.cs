using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for bulk updating product partial surcharge configurations.
/// </summary>
public class UpdateProductPartialSurchargesDto
{
    [Required]
    public long ProductId { get; set; }

    /// <summary>
    /// List of surcharge configurations to set for the product.
    /// </summary>
    public List<CreateProductPartialSurchargeDto> Surcharges { get; set; } = new();
}
