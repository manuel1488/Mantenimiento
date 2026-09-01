using App.Core.Common;
using App.Core.Enums.Notifications;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Core.Models.Notifications;

namespace App.Services.Notifications.Channels;

/// <summary>Delivers a <see cref="NotificationMessage"/> via the Telegram Bot API.</summary>
public class TelegramNotificationChannel : INotificationChannel
{
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly ITelegramSettingsService _telegramSettingsService;

    public TelegramNotificationChannel(ITelegramApiClient telegramApiClient, ITelegramSettingsService telegramSettingsService)
    {
        _telegramApiClient = telegramApiClient;
        _telegramSettingsService = telegramSettingsService;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.Telegram;

    public bool CanSend(NotificationMessage message) =>
        message.Recipients.TryGetValue(NotificationChannelType.Telegram, out var chatId) &&
        !string.IsNullOrWhiteSpace(chatId);

    public async Task<Result> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var settings = await _telegramSettingsService.GetSettingsAsync();
        if (settings is not { Enabled: true, BotToken.Length: > 0 })
            return Result.Failure("Telegram settings not configured or disabled");

        var text = $"{message.Subject}\n\n{message.Body}";
        return await _telegramApiClient.SendMessageAsync(
            settings.BotToken!, message.Recipients[NotificationChannelType.Telegram], text, cancellationToken);
    }
}
