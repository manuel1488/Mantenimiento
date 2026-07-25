namespace App.Core.DTOs.Settings;

public class InventorySettingsDto
{
    public int Id { get; set; }
    public bool ShowStockDuringPhysicalCount { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
