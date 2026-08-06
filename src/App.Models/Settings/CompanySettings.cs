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
    /// Main brand logo (full color), shown in the NavMenu, Login screen, and business
    /// documents (quotations, remissions, transfers, counts, sales reports). Distinct from
    /// <c>TicketConfiguration.CompanyLogoBase64</c>, which may be a simplified/B&amp;W variant
    /// optimized for thermal printing.
    /// </summary>
    public string? LogoBase64 { get; set; }

    /// <summary>
    /// When true, product prices are shown with tax (IVA) included in the sales terminal and receipts.
    /// </summary>
    public bool ShowPricesWithTax { get; set; } = false;
}