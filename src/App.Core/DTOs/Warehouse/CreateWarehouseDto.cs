using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Warehouse;

public class CreateWarehouseDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsPublicSalesWarehouse { get; set; } = false;
}