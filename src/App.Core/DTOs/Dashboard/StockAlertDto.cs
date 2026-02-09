namespace App.Core.DTOs.Dashboard;

public class StockAlertDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal? MinStock { get; set; }
    public string AlertType { get; set; } = string.Empty; // "OUT_OF_STOCK" or "LOW_STOCK"
    public string WarehouseName { get; set; } = string.Empty;
}