using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_discount_settings")]
public class DiscountSettings : BaseEntity<int>, IAuditTracked
{
    [Required]
    public bool RequireAuthorizationForPublicDiscount { get; set; } = true;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaximumPublicDiscount { get; set; } = 15;
}
