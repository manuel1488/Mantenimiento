using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Billing;


[Table("mx_pac_settings")]
public class MexicoPacSettings : BaseEntity<int>
{
    [Required]
    [StringLength(50)]
    public string ProviderName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string User { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string ProductionUrl { get; set; } = null!;

    [StringLength(200)]
    public string? TestUrl { get; set; }
}