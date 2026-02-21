using App.Core.Enums.Shop;

namespace App.Core.DTOs.Settings;

public class PaymentMethodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public PaymentMethodType Type { get; set; }
    public CardSubtype? CardSubtype { get; set; }
    public string? MxCfdiFormCode { get; set; }
    public string Icon { get; set; } = "payments";
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
