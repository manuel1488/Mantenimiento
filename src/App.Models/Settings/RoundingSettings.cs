using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Settings;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_rounding_settings")]
public class RoundingSettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>
    /// Whether rounding is enabled for sales
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Rounding method: Ceiling (up), Floor (down), Nearest (arithmetic)
    /// </summary>
    [Required]
    public RoundingMethod Method { get; set; } = RoundingMethod.Ceiling;

    /// <summary>
    /// Number of decimal places to round to (0=whole numbers, 1, 2)
    /// </summary>
    [Required]
    [Range(0, 2)]
    public int DecimalPlaces { get; set; } = 0;

    /// <summary>
    /// Minimum amount threshold - rounding only applies if total exceeds this
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal MinimumThreshold { get; set; } = 0;
}
