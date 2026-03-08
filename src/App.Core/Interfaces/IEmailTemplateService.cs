namespace App.Core.Interfaces;

/// <summary>
/// Service for managing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Gets an email template by name and replaces its placeholders with the provided data
    /// </summary>
    Task<string> GetTemplateAsync(string templateName, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available template names
    /// </summary>
    Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a static file from wwwroot and returns it as a base64 data URL (e.g. "data:image/webp;base64,...").
    /// Returns empty string if the file does not exist.
    /// </summary>
    Task<string> GetStaticFileBase64Async(string relativePath);

    /// <summary>
    /// Reads a static file from wwwroot and returns its raw bytes and MIME type.
    /// Returns (empty array, empty string) if the file does not exist.
    /// </summary>
    Task<(byte[] Bytes, string MimeType)> GetStaticFileBytesAsync(string relativePath);
}