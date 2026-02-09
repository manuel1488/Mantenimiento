using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Warehouse;

public class WarehouseDto : AuditableDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublicSalesWarehouse { get; set; }
}