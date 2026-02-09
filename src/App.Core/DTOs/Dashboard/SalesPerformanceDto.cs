namespace App.Core.DTOs.Dashboard;

public class SalesPerformanceDto
{
    public List<DailySalesDto> DailySales { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal PreviousPeriodRevenue { get; set; }
    public decimal RevenueGrowth { get; set; }
    public int TotalOrders { get; set; }
    public int PreviousPeriodOrders { get; set; }
    public decimal OrdersGrowth { get; set; }
}