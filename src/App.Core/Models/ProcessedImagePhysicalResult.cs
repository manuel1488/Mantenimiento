
namespace App.Core.Models;

/// <summary>
/// Result of image processing operation with physical file paths
/// </summary>
public class ProcessedImagePhysicalResult
{
    public string FileName { get; set; } = null!;
    public string ThumbnailFileName { get; set; } = null!;
    public string ImagePath { get; set; } = null!;
    public string ThumbnailPath { get; set; } = null!;
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public long ThumbnailSize { get; set; }
    public decimal CompressionRatio => OriginalSize > 0 ? (decimal)CompressedSize / OriginalSize : 1m;
}