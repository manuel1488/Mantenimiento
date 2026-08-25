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
    /// Allowed file extensions (with leading dot), the single source of truth for allowed image
    /// formats — used both for server-side validation (against the uploaded file name) and to
    /// build the native file picker's "accept" filter, so there's nothing to keep in sync.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = new[]
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    /// <summary>
    /// JPEG compression quality (0-100)
    /// </summary>
    public int JpegQuality { get; set; } = 75;

    /// <summary>
    /// Maximum width/height (px) for the full (non-thumbnail) processed image — larger uploads are
    /// downscaled preserving aspect ratio before compression; smaller images are never upscaled.
    /// Quality compression alone barely shrinks a 4000px+ phone photo, and nothing here displays it
    /// past the photo viewer's max-height:60vh, so keeping the original resolution has no upside.
    /// </summary>
    public int MaxWidth { get; set; } = 1280;

    public int MaxHeight { get; set; } = 1280;

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