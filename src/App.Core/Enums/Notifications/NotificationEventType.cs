namespace App.Core.Enums.Notifications;

/// <summary>
/// Catalog of business events a user can subscribe to for internal alerts (e.g. Telegram).
/// <see cref="ObraVencida"/> and <see cref="ActividadVencida"/> exist as forward-looking catalog
/// entries only — the system has no due-date field or overdue-detection job yet, so nothing
/// dispatches them today.
/// </summary>
public enum NotificationEventType
{
    ObraIniciada = 1,
    ObraFinalizada = 2,
    ObraVencida = 3,
    CotizacionAprobada = 4,
    CotizacionRechazada = 5,
    ActividadVencida = 6
}
