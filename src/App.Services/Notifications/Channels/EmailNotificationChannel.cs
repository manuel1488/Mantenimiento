using App.Core.Common;
using App.Core.Enums.Notifications;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Core.Models.Notifications;

namespace App.Services.Notifications.Channels;

/// <summary>Delivers a <see cref="NotificationMessage"/> as an email via <see cref="IEmailService"/>.</summary>
public class EmailNotificationChannel : INotificationChannel
{
    private readonly IEmailService _emailService;

    public EmailNotificationChannel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public bool CanSend(NotificationMessage message) =>
        message.Recipients.TryGetValue(NotificationChannelType.Email, out var address) &&
        !string.IsNullOrWhiteSpace(address);

    public async Task<Result> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var emailMessage = new Core.Models.Email.EmailMessage
        {
            To = message.Recipients[NotificationChannelType.Email],
            Subject = message.Subject,
            Body = message.Body,
            IsHtml = false,
            Attachments = message.Attachments
                .Select(a => new Core.Models.Email.EmailAttachment
                {
                    FileName = a.FileName,
                    Content = a.Content,
                    ContentType = a.ContentType
                })
                .ToList()
        };

        var result = await _emailService.SendAsync(emailMessage, cancellationToken);
        return result.Success ? Result.Success() : Result.Failure(result.Error ?? "Error sending email");
    }
}
