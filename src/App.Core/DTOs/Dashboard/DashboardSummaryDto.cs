namespace App.Core.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TodaySales { get; set; }
    public int TodaySalesCount { get; set; }
    public decimal WeekSales { get; set; }
    public int WeekSalesCount { get; set; }
    public decimal MonthSales { get; set; }
    public int MonthSalesCount { get; set; }

    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }

    public decimal AverageOrderValue { get; set; }
    public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new();
    public Dictionary<string, int> SalesByType { get; set; } = new();
}