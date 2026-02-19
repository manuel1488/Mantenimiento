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
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
