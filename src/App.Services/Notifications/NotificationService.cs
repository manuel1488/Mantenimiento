using App.Core.Common;
using App.Core.Interfaces.Notifications;
using App.Core.Models.Notifications;
using App.Models.Data.Contexts;
using App.Models.Notifications;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Notifications;

/// <summary>
/// Fans a <see cref="NotificationMessage"/> out to every registered <see cref="INotificationChannel"/>
/// that can handle it (Strategy pattern). Delivery is best-effort per channel — a failing channel
/// only logs and records the attempt, it never blocks or rolls back the caller's business state.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public NotificationService(
        IEnumerable<INotificationChannel> channels,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<NotificationService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService)
    {
        _channels = channels;
        _contextFactory = contextFactory;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> NotifyAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var applicableChannels = _channels.Where(c => c.CanSend(message)).ToList();
        if (applicableChannels.Count == 0)
        {
            _logger.LogWarning(
                "No notification channel available for event {EventType} on {RelatedEntityType} {RelatedEntityId}",
                message.EventType, message.RelatedEntityType, message.RelatedEntityId);
            return Result.Failure("No notification channel available for this message");
        }

        var anySucceeded = false;
        foreach (var channel in applicableChannels)
        {
            Result result;
            try
            {
                result = await channel.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification channel {Channel} threw while sending event {EventType}",
                    channel.ChannelType, message.EventType);
                result = Result.Failure(ex.Message);
            }

            if (result.IsSuccess)
                anySucceeded = true;
            else
                _logger.LogWarning("Notification channel {Channel} failed for event {EventType}: {Error}",
                    channel.ChannelType, message.EventType, result.Error);

            await LogAttemptAsync(message, channel.ChannelType, result, cancellationToken);
        }

        return anySucceeded ? Result.Success() : Result.Failure("All notification channels failed");
    }

    private async Task LogAttemptAsync(
        NotificationMessage message,
        Core.Enums.Notifications.NotificationChannelType channel,
        Result result,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var log = new NotificationLog
            {
                EventType = message.EventType,
                Channel = channel,
                RecipientAddress = message.Recipients.GetValueOrDefault(channel, string.Empty),
                Subject = message.Subject,
                Success = result.IsSuccess,
                ErrorMessage = result.IsSuccess ? null : result.Error,
                RelatedEntityType = message.RelatedEntityType,
                RelatedEntityId = message.RelatedEntityId,
                SentAt = _dateTimeService.Now,
                CreatedBy = await _currentUserService.GetUserIdAsync(),
                CreatedAt = _dateTimeService.Now
            };

            context.NotificationLogs.Add(log);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting notification log for event {EventType}", message.EventType);
        }
    }
}
