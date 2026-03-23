using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;

namespace App.Models.Settings;

[Table("stg_wholesale_settings")]
public class WholesaleSettings : BaseEntity<int>
{
    public WholesalePriceMode PriceMode { get; set; } = WholesalePriceMode.Percentage;

    public bool ApplyWholesaleToRemissions { get; set; } = false;
}
