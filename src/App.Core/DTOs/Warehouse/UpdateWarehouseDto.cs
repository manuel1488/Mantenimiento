using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Warehouse;

public class UpdateWarehouseDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public int? BranchId { get; set; }
}