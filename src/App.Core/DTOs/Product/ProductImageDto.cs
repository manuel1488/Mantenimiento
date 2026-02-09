using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Product;

public class ProductImageDto : AuditableDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string? ThumbnailFileName { get; set; }
    public bool IsPrimary { get; set; }
}