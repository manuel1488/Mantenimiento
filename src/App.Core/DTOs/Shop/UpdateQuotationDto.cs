using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class UpdateQuotationDto
{
    [Required]
    public long CustomerId { get; set; }

    public DateTime? QuoteDate { get; set; }

    public DateTime? ValidUntil { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateQuotationDetailDto> Details { get; set; } = [];
}
