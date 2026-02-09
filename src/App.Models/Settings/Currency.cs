using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_currencies")]
public class Currency : BaseEntity<int>
{
    [Required]
    [StringLength(3)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Symbol { get; set; } = null!;

    public bool IsActive { get; set; }
}