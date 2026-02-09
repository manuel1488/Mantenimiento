namespace App.Core.Utils;

/// <summary>
/// Simple utility for converting between image MIME types and file extensions
/// </summary>
public static class ImageTypeUtils
{
    /// <summary>
    /// Gets file extension from MIME type
    /// </summary>
    /// <param name="mimeType">The MIME type (e.g., "image/jpeg")</param>
    /// <returns>The file extension (e.g., ".jpg") or null if not found</returns>
    public static string? GetExtensionFromMimeType(string? mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => null
        };
    }

    /// <summary>
    /// Gets MIME type from file extension
    /// </summary>
    /// <param name="extension">The file extension (e.g., ".jpg" or "jpg")</param>
    /// <returns>The MIME type (e.g., "image/jpeg") or null if not found</returns>
    public static string? GetMimeTypeFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        var ext = extension.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
            ext = $".{ext}";

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => null
        };
    }

    /// <summary>
    /// Gets MIME type from filename
    /// </summary>
    /// <param name="fileName">The filename (e.g., "photo.jpg")</param>
    /// <returns>The MIME type or null if not recognized</returns>
    public static string? GetMimeTypeFromFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var extension = Path.GetExtension(fileName);
        return GetMimeTypeFromExtension(extension);
    }
}