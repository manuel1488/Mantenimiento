using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_discount_settings")]
public class DiscountSettings : BaseEntity<int>
{
    [Required]
    public bool RequireAuthorizationForPublicDiscount { get; set; } = true;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaximumPublicDiscount { get; set; } = 15;
}
