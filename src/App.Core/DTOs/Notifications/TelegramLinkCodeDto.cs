namespace App.Core.DTOs.Notifications;

public class TelegramLinkCodeDto
{
    public string Code { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string? BotUsername { get; set; }
}
