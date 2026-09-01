using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Core.Attributes;
using App.Core.Base;
using App.Core.Interfaces;

namespace App.Models.Settings;

[Table("stg_telegram_settings")]
public class TelegramSettings : BaseEntity<int>, IAuditTracked
{
    [StringLength(100)]
    [SensitiveData]
    public string? BotToken { get; set; }

    [StringLength(50)]
    public string? BotUsername { get; set; }

    [StringLength(200)]
    public string? WebhookBaseUrl { get; set; }

    [StringLength(100)]
    [SensitiveData]
    public string? WebhookSecretToken { get; set; }

    public bool Enabled { get; set; }
}
