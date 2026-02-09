namespace App.Core.DTOs.Dashboard;

public class RecentSaleDto
{
    public long Id { get; set; }
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SaleType { get; set; } = string.Empty;
}