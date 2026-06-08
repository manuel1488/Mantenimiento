using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Enums.Shop;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_wholesale_settings")]
public class WholesaleSettings : BaseEntity<int>, IAuditTracked
{
    public WholesalePriceMode PriceMode { get; set; } = WholesalePriceMode.Percentage;

    public bool ApplyWholesaleToRemissions { get; set; } = false;
}
