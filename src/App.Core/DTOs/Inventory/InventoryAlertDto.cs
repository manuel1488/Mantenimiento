namespace App.Core.DTOs.Inventory;

public class InventoryAlertDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string ProductBrand { get; set; } = null!;
    public string? ProductDescription { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
    public string UnitMeasureName { get; set; } = null!;
    public string AlertType { get; set; } = null!; // "LOW_STOCK" or "OVER_STOCK"
}