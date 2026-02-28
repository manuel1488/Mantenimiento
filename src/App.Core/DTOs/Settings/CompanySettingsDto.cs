namespace App.Core.DTOs.Settings;

public class CompanySettingsDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public string CurrencyCode { get; set; } = null!;
    public string TimeZoneId { get; set; } = null!;
    public string? TimeZoneDisplayName { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}