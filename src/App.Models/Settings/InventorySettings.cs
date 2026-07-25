using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_inventory_settings")]
public class InventorySettings : BaseEntity<int>, IAuditTracked
{
    /// <summary>
    /// Whether the system stock quantity is shown while capturing a physical inventory count.
    /// Hiding it prevents the counter from being biased toward the expected value.
    /// </summary>
    [Required]
    public bool ShowStockDuringPhysicalCount { get; set; } = true;
}
