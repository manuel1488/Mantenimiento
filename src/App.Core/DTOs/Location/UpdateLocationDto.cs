using System.ComponentModel.DataAnnotations;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Location;

public class UpdateLocationDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public LocationType Type { get; set; }

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

    // Geographic coordinates
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Legacy address (optional, for backward compatibility)
    [StringLength(200)]
    public string? Address { get; set; }

    // Contact information
    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
}
