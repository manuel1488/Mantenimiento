namespace App.Core.DTOs.UnitMeasure;

public class UnitMeasureDto
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}