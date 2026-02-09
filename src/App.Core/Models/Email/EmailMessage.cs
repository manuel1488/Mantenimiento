namespace App.Core.Models.Email;

/// <summary>
/// Represents an email message with all its components
/// </summary>
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
}