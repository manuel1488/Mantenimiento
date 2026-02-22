using System.ComponentModel.DataAnnotations;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class CreateSalePaymentDto
{
    [Required]
    public int PaymentMethodId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [StringLength(4)]
    public string? CardLastFour { get; set; }

    [StringLength(20)]
    public string? AuthorizationCode { get; set; }

    public CardBrand? CardBrand { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }
}
