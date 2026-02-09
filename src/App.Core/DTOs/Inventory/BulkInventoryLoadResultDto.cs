namespace App.Core.DTOs.Inventory;

public class BulkInventoryLoadResultDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}