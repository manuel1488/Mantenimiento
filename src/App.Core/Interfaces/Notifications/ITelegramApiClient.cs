using App.Core.Common;

namespace App.Core.Interfaces.Notifications;

/// <summary>Thin wrapper over the Telegram Bot API, shared by the outbound <c>TelegramNotificationChannel</c>
/// and the inbound webhook controller (which needs to reply to a chat).</summary>
public interface ITelegramApiClient
{
    Task<Result> SendMessageAsync(string botToken, string chatId, string text, CancellationToken cancellationToken = default);

    /// <summary>Registers the bot's webhook URL with Telegram (<c>setWebhook</c>).</summary>
    Task<Result> SetWebhookAsync(string botToken, string webhookUrl, string secretToken, CancellationToken cancellationToken = default);

    /// <summary>Fetches the bot's own username (<c>getMe</c>), used to build the <c>t.me/&lt;username&gt;</c> link shown to users.</summary>
    Task<Result<string>> GetBotUsernameAsync(string botToken, CancellationToken cancellationToken = default);
}
