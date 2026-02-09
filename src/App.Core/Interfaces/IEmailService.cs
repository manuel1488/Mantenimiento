using App.Core.Models.Email;

namespace App.Core.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email
    /// </summary>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the email configuration
    /// </summary>
    Task<EmailResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test email to verify the configuration
    /// </summary>
    Task<EmailResult> SendTestEmailAsync(string to, CancellationToken cancellationToken = default);
}