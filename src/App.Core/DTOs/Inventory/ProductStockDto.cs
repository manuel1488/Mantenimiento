namespace App.Core.DTOs.Inventory;

public class ProductStockDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string UnitMeasureName { get; set; } = null!;
    public decimal TotalStock { get; set; }
    public List<ProductWarehouseStockDto> LocationStock { get; set; } = new();
}