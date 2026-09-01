using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Web.Controllers.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace App.Web.Controllers;

/// <summary>
/// Receives updates from Telegram for the internal-alerts bot. Called by Telegram itself, not by a
/// logged-in user, so it's anonymous and authenticated only via the shared secret Telegram echoes
/// back in the <c>X-Telegram-Bot-Api-Secret-Token</c> header (set via <c>setWebhook</c>).
///
/// Today this only recognizes a 6-digit account-linking PIN; any other message gets a generic
/// fallback reply. That fallback is the intended extension point for a future AI-driven reply.
/// </summary>
[ApiController]
[Route("api/telegram")]
[AllowAnonymous]
public partial class TelegramWebhookController : ControllerBase
{
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    private readonly ITelegramSettingsService _telegramSettingsService;
    private readonly ITelegramLinkService _telegramLinkService;
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly IStringLocalizer<TelegramWebhookController> _localizer;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ITelegramSettingsService telegramSettingsService,
        ITelegramLinkService telegramLinkService,
        ITelegramApiClient telegramApiClient,
        IStringLocalizer<TelegramWebhookController> localizer,
        ILogger<TelegramWebhookController> logger)
    {
        _telegramSettingsService = telegramSettingsService;
        _telegramLinkService = telegramLinkService;
        _telegramApiClient = telegramApiClient;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        var settings = await _telegramSettingsService.GetSettingsAsync();
        if (settings is not { Enabled: true, BotToken.Length: > 0, WebhookSecretToken.Length: > 0 })
            return NotFound();

        if (!Request.Headers.TryGetValue(SecretTokenHeader, out var receivedSecret) ||
            !FixedTimeEquals(receivedSecret.ToString(), settings.WebhookSecretToken!))
        {
            _logger.LogWarning("Rejected Telegram webhook call with invalid secret token");
            return Unauthorized();
        }

        // Telegram expects a fast 2xx ack regardless of what we do with the update.
        if (update.Message is not { Chat.Id: var chatId, Text: { Length: > 0 } text })
            return Ok();

        var chatIdString = chatId.ToString();

        if (PinCodeRegex().IsMatch(text.Trim()))
        {
            var linkResult = await _telegramLinkService.TryLinkAsync(text.Trim(), chatIdString, cancellationToken);
            var reply = linkResult.IsSuccess
                ? _localizer["Your account has been linked. You'll now receive alerts here."]
                : _localizer["That code is invalid or expired. Generate a new one from your profile."];

            await _telegramApiClient.SendMessageAsync(settings.BotToken!, chatIdString, reply, cancellationToken);
            return Ok();
        }

        // Fallback for anything else — extension point for a future AI-driven reply.
        await _telegramApiClient.SendMessageAsync(
            settings.BotToken!,
            chatIdString,
            _localizer["I don't recognize that message. To link your account, generate a code from your profile and send it here."],
            cancellationToken);

        return Ok();
    }

    [GeneratedRegex(@"^\d{6}$")]
    private static partial Regex PinCodeRegex();

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
