using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_bulk_label_jobs")]
public class BulkLabelJob : BaseEntity<long>
{
    public long ProductId { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(10)]
    public string UnitMeasureCode { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    public int LabelCount { get; set; } = 1;

    [StringLength(50)]
    public string? BatchNumber { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}
