using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_localization_settings")]
public class LocalizationSettings : BaseEntity<int>
{
    [Required]
    [StringLength(5)]
    public string DefaultLanguage { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string DefaultTimeZone { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string NumberFormat { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string DateFormat { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string TimeFormat { get; set; } = null!;
}