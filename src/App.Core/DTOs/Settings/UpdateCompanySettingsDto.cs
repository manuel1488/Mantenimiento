using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateCompanySettingsDto
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = null!;

    public string? LogoBase64 { get; set; }

    [Range(0, 100)]
    public decimal IvaTasaPorDefecto { get; set; } = 16.00m;

    [StringLength(300)]
    public string? Direccion { get; set; }
}