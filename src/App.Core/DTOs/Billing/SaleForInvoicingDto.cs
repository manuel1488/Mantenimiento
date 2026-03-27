namespace App.Core.DTOs.Billing;

public class SaleForInvoicingDto
{
    public long SaleId { get; set; }
    public decimal Total { get; set; }
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public bool CustomerSendInvoiceEmail { get; set; }
    public string? CustomerRfc { get; set; }
    public string? CustomerLegalName { get; set; }
    public string? CustomerPostalCode { get; set; }
    public string? CustomerFiscalRegime { get; set; }

    /// <summary>
    /// CFDI FormaPago code resolved from the sale's payment methods and the configured MultiPaymentFormPolicy.
    /// </summary>
    public string ResolvedPaymentForm { get; set; } = string.Empty;
}
