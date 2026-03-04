namespace App.Core.DTOs.Billing.Mexico;

public class CfdiPostalCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string StateId { get; set; } = null!;
    public string? MunicipalityId { get; set; }
    public string? LocalityId { get; set; }
    public bool IsBorderZone { get; set; }
    public string TimeZoneName { get; set; } = null!;
    public string IanaTimeZoneId { get; set; } = null!;
    public int OffsetWinter { get; set; }
    public int OffsetSummer { get; set; }
}
