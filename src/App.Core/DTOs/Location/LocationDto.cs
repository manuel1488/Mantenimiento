using App.Core.DTOs.Shared;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Location;

public class LocationDto : AuditableDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public LocationType Type { get; set; }
    public string TypeName => Type.ToString();
    public bool IsActive { get; set; }

    // Structured address fields
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "MX";

    // Geographic coordinates
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Legacy/computed address
    public string? Address { get; set; }

    // Contact information
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
