using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shop;

[Table("sh_product_images")]
public class ProductImage : BaseEntity<long>
{
    public long ProductId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(255)]
    public string? ThumbnailFileName { get; set; }

    [Required]
    public byte[] ImageData { get; set; } = null!;

    [Required]
    public byte[] ThumnailImageData { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string ContentType { get; set; } = null!;

    public bool IsPrimary { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}