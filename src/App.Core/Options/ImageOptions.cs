namespace App.Core.Options;

public class ImageOptions
{
    public const string SectionName = "Images";

    /// <summary>
    /// Base directory for storing images
    /// </summary>
    public string StoragePath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// Maximum file size in bytes (default: 5MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Allowed file types
    /// </summary>
    public string[] AllowedTypes { get; set; } = new[] 
    { 
        "image/jpeg", 
        "image/png", 
        "image/webp" 
    };

    /// <summary>
    /// JPEG compression quality (0-100)
    /// </summary>
    public int JpegQuality { get; set; } = 75;

    /// <summary>
    /// Thumbnail settings
    /// </summary>
    public ThumbnailOptions Thumbnail { get; set; } = new();

    public class ThumbnailOptions
    {
        public int MaxWidth { get; set; } = 300;
        public int MaxHeight { get; set; } = 300;
        public string Prefix { get; set; } = "thumb_";
    }
}