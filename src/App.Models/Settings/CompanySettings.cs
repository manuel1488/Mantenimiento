using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Settings;

[Table("stg_settings")]
public class CompanySettings : BaseEntity<int>
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = null!;

    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = null!;

    [StringLength(200)]
    public string? TimeZoneDisplayName { get; set; }

    /// <summary>
    /// When true, product prices are shown with tax (IVA) included in the sales terminal and receipts.
    /// </summary>
    public bool ShowPricesWithTax { get; set; } = false;
}