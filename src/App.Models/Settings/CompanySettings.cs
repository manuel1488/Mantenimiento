using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_settings")]
public class CompanySettings : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = "UTC";

    [StringLength(200)]
    public string? TimeZoneDisplayName { get; set; }

    /// <summary>
    /// Main brand logo (full color), shown in the NavMenu, Login screen, and generated documents.
    /// </summary>
    public string? LogoBase64 { get; set; }
}