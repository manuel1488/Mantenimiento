using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class CreateRemissionDto
{
    [Required]
    public long CustomerId { get; set; }

    public DateTime? RemissionDate { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    public long? QuotationId { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateRemissionDetailDto> Details { get; set; } = [];
}
