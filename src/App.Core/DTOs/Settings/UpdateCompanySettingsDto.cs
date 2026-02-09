using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateCompanySettingsDto
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
    [StringLength(50)]
    public string TimeZoneId { get; set; } = null!;
}