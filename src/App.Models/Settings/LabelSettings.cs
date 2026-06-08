using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_label_settings")]
public class LabelSettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>Label width in millimeters (e.g. 62 for Brother DK-2205).</summary>
    [Required]
    public int WidthMm { get; set; } = 62;

    /// <summary>Label height in millimeters. For continuous tape this is the cut height.</summary>
    [Required]
    public int HeightMm { get; set; } = 28;
}
