namespace App.Core.DTOs.Shop;

public class SaleCancellationStatusDto
{
    public bool CanCancel { get; set; }
    public bool BlockedByInvoice { get; set; }
    public bool BlockedByGlobalInvoice { get; set; }
    public long? GlobalInvoiceId { get; set; }
}
