using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_tax_settings")]
public class TaxSettings : BaseEntity<int>, IAuditTracked
{
    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string BusinessName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string TaxId { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string FiscalRegime { get; set; } = null!;

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    // Fields for Mexico
    [StringLength(5)]
    public string? MxDefaultCfdiUse { get; set; }

    [StringLength(5)]
    public string? MxDefaultPaymentMethod { get; set; }

    [StringLength(5)]
    public string? MxDefaultPaymentType { get; set; }

    // Fileds for Canada
    [StringLength(20)]
    public string? CaGstNumber { get; set; }

    [StringLength(20)]
    public string? CaPstNumber { get; set; }

    [StringLength(20)] 
    public string? CaHstNumber { get; set; }

    [StringLength(20)]
    public string? CaQstNumber { get; set; }

    // Postal code timezone info (populated from CFDI postal code catalog)
    [StringLength(100)]
    public string? PostalCodeTimeZoneName { get; set; }

    [StringLength(100)]
    public string? PostalCodeIanaTimeZoneId { get; set; }

    public int? PostalCodeOffsetWinter { get; set; }

    public int? PostalCodeOffsetSummer { get; set; }

    /// <summary>Maximum hours in the past an invoice date can be backdated. Null = no enforced limit.</summary>
    public int? MxMaxBackdateHours { get; set; }
}