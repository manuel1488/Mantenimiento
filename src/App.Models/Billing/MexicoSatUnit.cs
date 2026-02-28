using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Billing;

[Table("mx_sat_units")]
public class MexicoSatUnit : BaseEntity<int>
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string? Symbol { get; set; }
}
