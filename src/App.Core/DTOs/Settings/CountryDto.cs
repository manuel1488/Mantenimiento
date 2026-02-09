namespace App.Core.DTOs.Settings;

public class CountryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string DefaultCurrencyCode { get; set; } = null!;
    public bool IsActive { get; set; }
}