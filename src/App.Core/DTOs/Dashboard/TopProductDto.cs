namespace App.Core.DTOs.Dashboard;

public class TopProductDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}
