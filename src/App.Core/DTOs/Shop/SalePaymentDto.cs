using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class SalePaymentDto
{
    public long Id { get; set; }
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = null!;
    public PaymentMethodType PaymentMethodType { get; set; }
    public CardSubtype? CardSubtype { get; set; }
    public string PaymentMethodIcon { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? CardLastFour { get; set; }
    public string? AuthorizationCode { get; set; }
    public CardBrand? CardBrand { get; set; }
    public string? Reference { get; set; }
}
