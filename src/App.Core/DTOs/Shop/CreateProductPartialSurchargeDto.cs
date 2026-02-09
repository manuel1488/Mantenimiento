using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for creating/updating a product partial surcharge configuration.
/// </summary>
public class CreateProductPartialSurchargeDto
{
    [Required]
    public int PartialSaleFractionId { get; set; }

    [Range(0, 100, ErrorMessage = "Surcharge percentage must be between 0 and 100")]
    public decimal SurchargePercentage { get; set; }

    public bool IsActive { get; set; } = true;
}
