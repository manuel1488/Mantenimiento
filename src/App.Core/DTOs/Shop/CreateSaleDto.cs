using System.ComponentModel.DataAnnotations;
using App.Core.Constants;

namespace App.Core.DTOs.Shop;

public class CreateSaleDto
{
    [Required]
    public long CustomerId { get; set; }

    public DateTime? SaleDate { get; set; }

    public SaleType SaleType { get; set; } = SaleType.Public;

    public long? QuotationId { get; set; }

    public int? LocationId { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    public string? DiscountAuthorizedBy { get; set; }
    public string? DiscountAuthorizerId { get; set; }
    public DateTime? DiscountAuthorizedAt { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSalePaymentDto> Payments { get; set; } = new();

    [Required]
    public List<CreateSaleDetailDto> Details { get; set; } = new();
}
