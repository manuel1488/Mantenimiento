using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateInventorySettingsDto
{
    [Required]
    public bool ShowStockDuringPhysicalCount { get; set; }
}
