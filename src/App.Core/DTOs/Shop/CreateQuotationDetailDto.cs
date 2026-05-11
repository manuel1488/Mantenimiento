using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class CreateQuotationDetailDto
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

    /// Fixed discount amount for the whole line (used when wholesale rule is FixedPrice). When set, takes priority over DiscountPercentage.
    public decimal? DiscountAmount { get; set; }
}
