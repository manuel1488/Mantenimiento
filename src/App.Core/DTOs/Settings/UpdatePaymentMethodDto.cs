using System.ComponentModel.DataAnnotations;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Settings;

public class UpdatePaymentMethodDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public PaymentMethodType Type { get; set; }

    public CardSubtype? CardSubtype { get; set; }

    [StringLength(5)]
    public string? MxCfdiFormCode { get; set; }

    [StringLength(50)]
    public string Icon { get; set; } = "payments";

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }
}
