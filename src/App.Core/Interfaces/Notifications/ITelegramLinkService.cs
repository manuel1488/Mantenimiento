using App.Core.Common;
using App.Core.DTOs.Notifications;

namespace App.Core.Interfaces.Notifications;

/// <summary>
/// Links a Telegram chat to an <c>ApplicationUser</c> via a short-lived PIN the user generates in
/// their profile and sends to the bot.
/// </summary>
public interface ITelegramLinkService
{
    /// <summary>Generates a new PIN for the user, invalidating any previous unused one.</summary>
    Task<Result<TelegramLinkCodeDto>> GenerateLinkCodeAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called by the Telegram webhook when a chat sends a message matching the PIN shape. Validates
    /// the code (exists, not expired, not used), links <paramref name="chatId"/> to that user's
    /// <c>TelegramChatId</c>, and marks the code used. Returns the linked user's id on success.
    /// </summary>
    Task<Result<string>> TryLinkAsync(string code, string chatId, CancellationToken cancellationToken = default);

    /// <summary>Clears the current user's Telegram link.</summary>
    Task<Result> UnlinkAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Whether the user already has a Telegram chat linked.</summary>
    Task<bool> IsLinkedAsync(string userId, CancellationToken cancellationToken = default);
}
