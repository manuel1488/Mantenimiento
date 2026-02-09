using App.Core.Models.Email;

namespace App.Services.Email;

/// <summary>
/// Extension methods for email functionality
/// </summary>
public static class EmailExtensions
{
    /// <summary>
    /// Creates a new EmailMessage with HTML content
    /// </summary>
    public static EmailMessage CreateHtmlMessage(
        string to,
        string subject,
        string body)
    {
        return new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = true
        };
    }

    /// <summary>
    /// Creates a new EmailMessage with plain text content
    /// </summary>
    public static EmailMessage CreateTextMessage(
        string to,
        string subject,
        string body)
    {
        return new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = false
        };
    }

    /// <summary>
    /// Adds CC recipients to the message
    /// </summary>
    public static EmailMessage WithCc(this EmailMessage message, string cc)
    {
        message.Cc = cc;
        return message;
    }

    /// <summary>
    /// Adds BCC recipients to the message
    /// </summary>
    public static EmailMessage WithBcc(this EmailMessage message, string bcc)
    {
        message.Bcc = bcc;
        return message;
    }

    /// <summary>
    /// Adds an attachment to the message
    /// </summary>
    public static EmailMessage WithAttachment(
        this EmailMessage message,
        string fileName,
        byte[] content,
        string contentType)
    {
        message.Attachments.Add(new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType
        });
        return message;
    }

    /// <summary>
    /// Sets the priority of the message
    /// </summary>
    public static EmailMessage WithPriority(
        this EmailMessage message,
        EmailPriority priority)
    {
        message.Priority = priority;
        return message;
    }

    /// <summary>
    /// Adds a custom header to the message
    /// </summary>
    public static EmailMessage WithHeader(
        this EmailMessage message,
        string name,
        string value)
    {
        message.Headers[name] = value;
        return message;
    }
}