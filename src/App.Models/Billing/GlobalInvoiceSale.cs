using System.ComponentModel.DataAnnotations.Schema;

using App.Models.Shop;

namespace App.Models.Billing;

/// <summary>Junction table linking a GlobalInvoice to the Sales it consolidates.</summary>
[Table("mx_global_invoice_sales")]
public class GlobalInvoiceSale
{
    public long GlobalInvoiceId { get; set; }
    public long SaleId { get; set; }

    [ForeignKey(nameof(GlobalInvoiceId))]
    public virtual GlobalInvoice GlobalInvoice { get; set; } = null!;

    [ForeignKey(nameof(SaleId))]
    public virtual Sale Sale { get; set; } = null!;
}
