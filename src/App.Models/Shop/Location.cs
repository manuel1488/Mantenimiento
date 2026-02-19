using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Core.Enums.Shop;

namespace App.Models.Shop;

/// <summary>
/// Unified storage location entity (Warehouse or Branch)
/// </summary>
[Table("sh_locations")]
public class Location : BaseEntity<int>
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public LocationType Type { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }
}
