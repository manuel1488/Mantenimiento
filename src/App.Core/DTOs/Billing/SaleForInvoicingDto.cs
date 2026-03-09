namespace App.Core.DTOs.Billing;

public class SaleForInvoicingDto
{
    public long SaleId { get; set; }
    public decimal Total { get; set; }
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public bool CustomerSendInvoiceEmail { get; set; }
}
