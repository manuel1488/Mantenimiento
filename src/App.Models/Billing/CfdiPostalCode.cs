using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;

namespace App.Models.Billing;

[Table("cat_cfdi_postal_codes")]
public class CfdiPostalCode : BaseEntity<int>
{
    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(3)]
    public string StateId { get; set; } = null!;

    [StringLength(50)]
    public string? MunicipalityId { get; set; }

    [StringLength(10)]
    public string? LocalityId { get; set; }

    public bool IsBorderZone { get; set; }

    [Required]
    [StringLength(100)]
    public string TimeZoneName { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string IanaTimeZoneId { get; set; } = null!;

    public int OffsetWinter { get; set; }

    public int OffsetSummer { get; set; }
}
