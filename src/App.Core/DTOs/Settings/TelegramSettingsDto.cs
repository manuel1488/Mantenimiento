namespace App.Core.DTOs.Settings;

public class TelegramSettingsDto
{
    public int Id { get; set; }
    public string? BotToken { get; set; }
    public string? BotUsername { get; set; }
    public string? WebhookBaseUrl { get; set; }
    public string? WebhookSecretToken { get; set; }
    public bool Enabled { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    /// <summary>Transient, not persisted: set by <c>UpdateSettingsAsync</c> when <c>setWebhook</c> failed, so the UI can surface it.</summary>
    public string? WebhookRegistrationError { get; set; }
}
