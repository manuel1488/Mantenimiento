using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;

namespace App.Models.Settings;

[Table("stg_payment_methods")]
public class PaymentMethod : BaseEntity<int>
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    public PaymentMethodType Type { get; set; }

    public CardSubtype? CardSubtype { get; set; }

    /// <summary>
    /// Links to mx_payment_forms.code for Mexico CFDI billing.
    /// </summary>
    [StringLength(5)]
    public string? MxCfdiFormCode { get; set; }

    /// <summary>
    /// Material Icons name used in the point-of-sale UI.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Icon { get; set; } = "payments";

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
