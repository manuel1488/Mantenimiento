using App.Core.Interfaces;
using App.Core.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace App.Services.Email;

/// <summary>
/// Implementation of email service using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailSettingsService _settingsService;
    private readonly ILogger<EmailService> _logger;
    private readonly IStringLocalizer<EmailService> _localizer;

    public EmailService(
        IEmailSettingsService settingsService,
        ILogger<EmailService> logger,
        IStringLocalizer<EmailService> localizer)
    {
        _settingsService = settingsService;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings == null)
                return EmailResult.Failed(_localizer["Email settings not configured"]);

            var mimeMessage = new MimeMessage();

            // Set From address
            mimeMessage.From.Add(new MailboxAddress(
                settings.FromName ?? settings.FromEmail,
                settings.FromEmail!));

            // Set To address
            foreach (var address in message.To.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                mimeMessage.To.Add(MailboxAddress.Parse(address.Trim()));
            }

            // Set CC addresses if any
            if (!string.IsNullOrEmpty(message.Cc))
            {
                foreach (var address in message.Cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mimeMessage.Cc.Add(MailboxAddress.Parse(address.Trim()));
                }
            }

            // Set BCC addresses if any
            if (!string.IsNullOrEmpty(message.Bcc))
            {
                foreach (var address in message.Bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mimeMessage.Bcc.Add(MailboxAddress.Parse(address.Trim()));
                }
            }

            // Set subject
            mimeMessage.Subject = message.Subject;

            // Set priority
            mimeMessage.Priority = message.Priority switch
            {
                EmailPriority.High => MessagePriority.Urgent,
                EmailPriority.Low => MessagePriority.NonUrgent,
                _ => MessagePriority.Normal
            };

            // Create message body
            var builder = new BodyBuilder();
            if (message.IsHtml)
                builder.HtmlBody = message.Body;
            else
                builder.TextBody = message.Body;

            // Add attachments if any
            foreach (var attachment in message.Attachments)
            {
                builder.Attachments.Add(attachment.FileName, 
                    attachment.Content, 
                    ContentType.Parse(attachment.ContentType));
            }

            mimeMessage.Body = builder.ToMessageBody();

            // Add custom headers if any
            foreach (var header in message.Headers)
            {
                mimeMessage.Headers.Add(header.Key, header.Value);
            }

            // Send email
            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(
                settings.SmtpHost,
                settings.SmtpPort ?? 0,
                settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);

            if (!string.IsNullOrEmpty(settings.SmtpUser))
            {
                await client.AuthenticateAsync(
                    settings.SmtpUser,
                    settings.SmtpPassword,
                    cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return EmailResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", message.To);
            return EmailResult.Failed(ex);
        }
    }

    public async Task<EmailResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings == null)
                return EmailResult.Failed(_localizer["Email settings not configured"]);

            if (string.IsNullOrEmpty(settings.SmtpHost))
                return EmailResult.Failed(_localizer["SMTP host is required"]);

            if (!settings.SmtpPort.HasValue)
                return EmailResult.Failed(_localizer["SMTP port is required"]);

            if (string.IsNullOrEmpty(settings.FromEmail))
                return EmailResult.Failed(_localizer["From email is required"]);

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(
                settings.SmtpHost,
                settings.SmtpPort.Value,
                settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);

            if (!string.IsNullOrEmpty(settings.SmtpUser))
            {
                await client.AuthenticateAsync(
                    settings.SmtpUser,
                    settings.SmtpPassword,
                    cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);

            return EmailResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email configuration");
            return EmailResult.Failed(ex);
        }
    }

    public async Task<EmailResult> SendTestEmailAsync(string to, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            To = to,
            Subject = _localizer["Test Email - {0}", DateTime.Now.ToString("g")],
            Body = _localizer["This is a test email to verify the SMTP configuration."],
            IsHtml = false,
            Priority = EmailPriority.Normal
        };

        return await SendAsync(message, cancellationToken);
    }
}