namespace App.Core.DTOs.Settings;

public class CurrencyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public bool IsActive { get; set; }
}