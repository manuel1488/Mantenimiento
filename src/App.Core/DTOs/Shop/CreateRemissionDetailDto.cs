using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class CreateRemissionDetailDto
{
    [Required]
    public long ProductId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    /// <summary>Fixed discount amount override. When set, used directly instead of recomputing from DiscountPercentage.</summary>
    public decimal? DiscountAmount { get; set; }
}
