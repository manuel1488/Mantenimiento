using System.ComponentModel.DataAnnotations;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Location;

public class CreateLocationDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public LocationType Type { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
}
