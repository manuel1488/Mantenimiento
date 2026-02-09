namespace App.Core.DTOs.Inventory;

public class ProductWarehouseStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
}