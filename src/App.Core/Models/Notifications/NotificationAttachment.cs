namespace App.Core.Models.Notifications;

/// <summary>
/// A file attached to a <see cref="NotificationMessage"/>. Whether a channel actually delivers
/// it depends on the channel (e.g. email attaches it as a MIME part; a channel with no
/// attachment support may just ignore this list).
/// </summary>
public class NotificationAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
}
