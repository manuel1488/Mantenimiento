using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;
using App.Models.Shop;

namespace App.Models.Billing;


[Table("mx_invoices")]
public class MexicoInvoice : BaseEntity<long>
{
    public long SaleId { get; set; }

    [Required]
    [StringLength(5)]
    public string CfdiUse { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string PaymentMethod { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string PaymentType { get; set; } = null!;

    [Required]
    [StringLength(5)]
    public string FiscalRegime { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = null!;

    public DateTime? CancellationDate { get; set; }

    [StringLength(100)]
    public string? CancellationReason { get; set; }

    [ForeignKey(nameof(SaleId))]
    public virtual Sale Sale { get; set; } = null!;

    public virtual ICollection<MexicoInvoiceFile> Files { get; set; } = new List<MexicoInvoiceFile>();
}