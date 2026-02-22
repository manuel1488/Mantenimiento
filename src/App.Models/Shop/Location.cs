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

    // Structured address fields
    [StringLength(200)]
    public string? Street { get; set; }

    [StringLength(20)]
    public string? ExteriorNumber { get; set; }

    [StringLength(20)]
    public string? InteriorNumber { get; set; }

    [StringLength(100)]
    public string? Neighborhood { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(2)]
    public string Country { get; set; } = "MX";

    // Geographic coordinates (optional)
    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitude { get; set; }

    // Legacy/computed address field for backward compatibility
    [StringLength(200)]
    public string? Address { get; set; }

    // Contact information
    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public virtual ICollection<CashStation> CashStations { get; set; } = [];
}
