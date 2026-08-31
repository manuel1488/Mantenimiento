using App.Core.Enums.Notifications;
using App.Models.Clientes;

namespace App.Services.Notifications;

/// <summary>
/// Builds the notification recipient map from whatever contact data a Cliente has on file —
/// channel-agnostic by design, so adding a new <see cref="App.Core.Interfaces.Notifications.INotificationChannel"/>
/// (e.g. WhatsApp) starts working for existing Clientes without touching any caller of this class.
/// </summary>
public static class ClienteNotificationRecipients
{
    public static Dictionary<NotificationChannelType, string> Build(Cliente cliente)
    {
        var recipients = new Dictionary<NotificationChannelType, string>();

        if (!string.IsNullOrWhiteSpace(cliente.Correo))
            recipients[NotificationChannelType.Email] = cliente.Correo;

        if (!string.IsNullOrWhiteSpace(cliente.Telefono))
            recipients[NotificationChannelType.WhatsApp] = cliente.Telefono;

        return recipients;
    }

    public static string Describe(IReadOnlyDictionary<NotificationChannelType, string> recipients)
        => string.Join("; ", recipients.Select(r => $"{r.Key}: {r.Value}"));
}
