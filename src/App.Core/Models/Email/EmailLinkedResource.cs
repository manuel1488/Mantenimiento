namespace App.Core.Models.Email;

/// <summary>
/// Represents an embedded/linked resource (e.g. inline image) in an HTML email
/// </summary>
public class EmailLinkedResource
{
    public string ContentId { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}
