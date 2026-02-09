using App.Core.DTOs.Shop;

namespace App.Core.DTOs.Reports;

public class SalesSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalDiscount { get; set; }
    public Dictionary<string, int> SalesByType { get; set; } = new();
    public Dictionary<string, int> SalesByStatus { get; set; } = new();
    public Dictionary<string, int> SalesByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> SalesByPaymentMethodAmount { get; set; } = new();
    public List<SaleDto> TopSales { get; set; } = new();
    public List<SaleGroupedByDateDto> SalesByDate { get; set; } = new();
}

public class SaleGroupedByDateDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Total { get; set; }
}