using App.Core.Enums.Settings;

namespace App.Core.DTOs.Settings;

public class RoundingSettingsDto
{
    public int Id { get; set; }
    public bool IsEnabled { get; set; }
    public RoundingMethod Method { get; set; }
    public int DecimalPlaces { get; set; }
    public decimal MinimumThreshold { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
