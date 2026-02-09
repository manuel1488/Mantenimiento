namespace App.Core.DTOs.Settings;

public class TaxRateDto
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
    public string? ProvinceCode { get; set; }
    public string? Type { get; set; }
    public bool IsActive { get; set; }
}