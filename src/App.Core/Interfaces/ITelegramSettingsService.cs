using App.Core.Common;
using App.Core.DTOs.Settings;

namespace App.Core.Interfaces;

public interface ITelegramSettingsService
{
    /// <summary>Gets the current Telegram settings if they exist. <see cref="TelegramSettingsDto.BotToken"/> is never masked here — callers that render it in UI must mask it themselves.</summary>
    Task<TelegramSettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Updates the Telegram settings (creating them if they don't exist yet). If <see cref="UpdateTelegramSettingsDto.Enabled"/>
    /// is true and a bot token is present, re-registers the webhook with Telegram (<c>setWebhook</c>) against the
    /// configured <see cref="UpdateTelegramSettingsDto.WebhookBaseUrl"/> and refreshes the cached bot username (<c>getMe</c>).
    /// </summary>
    Task<TelegramSettingsDto> UpdateSettingsAsync(UpdateTelegramSettingsDto updateDto);

    /// <summary>Validates a bot token against Telegram's <c>getMe</c> without saving anything, returning the bot's username on success.</summary>
    Task<Result<string>> TestBotTokenAsync(string botToken, CancellationToken cancellationToken = default);
}
