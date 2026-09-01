using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using App.Core.Common;
using App.Core.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace App.Services.Notifications;

/// <summary>Thin wrapper over <c>https://api.telegram.org/bot{token}/...</c>.</summary>
public class TelegramApiClient : ITelegramApiClient
{
    public const string HttpClientName = "TelegramBot";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramApiClient> _logger;

    public TelegramApiClient(IHttpClientFactory httpClientFactory, ILogger<TelegramApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Builds a relative request path. Telegram bot tokens contain a ':' (e.g. "123456:AAH...") — a
    /// path like "bot123456:AAH.../getMe" gets misread by Uri parsing as an absolute URI with scheme
    /// "bot123456" ("scheme is not supported"). A leading '/' rules that out (schemes can't start
    /// with '/'), so HttpClient correctly treats it as relative and resolves it against BaseAddress.
    /// </summary>
    private static string BotPath(string botToken, string method) => $"/bot{botToken}/{method}";

    public async Task<Result> SendMessageAsync(string botToken, string chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var response = await client.PostAsJsonAsync(
                BotPath(botToken, "sendMessage"),
                new { chat_id = chatId, text },
                cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode && payload?.Ok == true)
                return Result.Success();

            return Result.Failure(payload?.Description ?? $"Telegram API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Telegram sendMessage for chat {ChatId}", chatId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> SetWebhookAsync(string botToken, string webhookUrl, string secretToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var response = await client.PostAsJsonAsync(
                BotPath(botToken, "setWebhook"),
                new { url = webhookUrl, secret_token = secretToken },
                cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<TelegramApiResponse>(cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode && payload?.Ok == true)
                return Result.Success();

            return Result.Failure(payload?.Description ?? $"Telegram API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Telegram setWebhook for url {Url}", webhookUrl);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<string>> GetBotUsernameAsync(string botToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync(BotPath(botToken, "getMe"), cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<TelegramGetMeResponse>(cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode && payload?.Ok == true && payload.Result?.Username is { Length: > 0 } username)
                return Result<string>.Success(username);

            return Result<string>.Failure(payload?.Description ?? $"Telegram API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Telegram getMe");
            return Result<string>.Failure(ex.Message);
        }
    }

    private class TelegramApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private class TelegramGetMeResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("result")]
        public TelegramUser? Result { get; set; }
    }

    private class TelegramUser
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
