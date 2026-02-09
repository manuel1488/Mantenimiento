using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Inventory;

public class InventoryDto : AuditableDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductBrand { get; set; } = null!;
    public string? ProductDescription { get; set; }
    public string ProductCode { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal IndividualUnits { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
    public string UnitMeasureName { get; set; } = null!;
    public decimal ProductContent { get; set; }
}