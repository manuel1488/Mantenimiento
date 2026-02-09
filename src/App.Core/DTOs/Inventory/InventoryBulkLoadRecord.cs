namespace App.Core.DTOs.Inventory;

public class InventoryBulkLoadRecord
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
}