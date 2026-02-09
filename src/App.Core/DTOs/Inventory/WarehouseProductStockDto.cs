namespace App.Core.DTOs.Inventory;

public class WarehouseProductStockDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string UnitMeasureName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
    public bool IsBelowMinStock { get; set; }
    public bool IsAboveMaxStock { get; set; }
}