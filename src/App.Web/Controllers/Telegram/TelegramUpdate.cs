using System.Text.Json.Serialization;

namespace App.Web.Controllers.Telegram;

/// <summary>Minimal subset of Telegram's <c>Update</c> object — only what the webhook needs today.</summary>
public class TelegramUpdate
{
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat Chat { get; set; } = null!;
}

public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
