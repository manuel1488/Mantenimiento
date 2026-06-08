using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_tax_rates")]
public class TaxRate : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Rate { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsDefault { get; set; }

    // For Canada: province code
    [StringLength(2)]
    public string? ProvinceCode { get; set; }

    // For Mexico: VAT type (general, border, etc.)
    [StringLength(20)]
    public string? Type { get; set; }

    public bool IsActive { get; set; }
}