namespace App.Core.DTOs.Inventory;

public class WarehouseStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public int TotalProducts { get; set; }
    public int ProductsWithStock { get; set; }
    public int ProductsBelowMinStock { get; set; }
    public int ProductsAboveMaxStock { get; set; }
    public List<WarehouseProductStockDto> ProductStock { get; set; } = new();
}