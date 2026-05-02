namespace App.Core.DTOs.Billing;

public class InvoiceReportRequestDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CustomerRfc { get; set; }
    public string? Status { get; set; }
    public int PageSize { get; set; } = 10000;
}
