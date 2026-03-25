using App.Core.Enums.Shop;

namespace App.Core.DTOs.Settings;

public class UpdateWholesaleSettingsDto
{
    public WholesalePriceMode PriceMode { get; set; }
    public bool ApplyWholesaleToRemissions { get; set; }
}
