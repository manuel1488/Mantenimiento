namespace App.Web.Services;

/// <summary>
/// Shares a file via the browser's native OS-level share sheet (Web Share API), so the user can
/// pick WhatsApp and have the file attach natively. No WhatsApp API/gateway is involved — this only
/// works on browsers that support sharing files (iOS/iPadOS Safari, Android Chrome); elsewhere it
/// falls back to opening WhatsApp with a pre-filled text message and no attachment.
/// </summary>
public interface IWhatsAppShareService
{
    /// <summary>
    /// Returns true if the native share sheet handled the file (attachment shared for real),
    /// false if the fallback text-only wa.me link was used instead.
    /// </summary>
    Task<bool> ShareFileAsync(byte[] fileBytes, string fileName, string contentType, string text);

    /// <summary>
    /// Same native-share-then-wa.me-fallback behavior as <see cref="ShareFileAsync"/>, for plain
    /// text content (e.g. a link) that has no file to attach.
    /// </summary>
    Task<bool> ShareTextAsync(string text);
}
