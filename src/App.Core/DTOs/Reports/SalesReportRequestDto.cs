using App.Core.Constants;

namespace App.Core.DTOs.Reports;

public class SalesReportRequestDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchString { get; set; }
    public long? CustomerId { get; set; }
    public string? Status { get; set; }
    public SaleType? SaleType { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IncludeDetails { get; set; } = false;
    public int PageSize { get; set; } = 50;
}