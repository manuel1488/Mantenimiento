using App.Core.Enums.Shop;

namespace App.Core.DTOs.Settings;

public class WholesaleSettingsDto
{
    public int Id { get; set; }
    public WholesalePriceMode PriceMode { get; set; }
}
