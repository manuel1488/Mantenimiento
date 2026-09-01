using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Settings;

public class UpdateTelegramSettingsDto
{
    [StringLength(100)]
    public string? BotToken { get; set; }

    [StringLength(200)]
    public string? WebhookBaseUrl { get; set; }

    public bool Enabled { get; set; }
}
