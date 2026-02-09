using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Billing;



[Table("mx_invoice_files")]
public class MexicoInvoiceFile : BaseEntity<long>
{
    public long InvoiceId { get; set; }

    [Required]
    [StringLength(10)]
    public string FileType { get; set; } = null!;

    [Required]
    public byte[] FileData { get; set; } = null!;

    [ForeignKey(nameof(InvoiceId))]
    public virtual MexicoInvoice Invoice { get; set; } = null!;
}