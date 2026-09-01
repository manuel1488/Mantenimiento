### ADR-011: Notificaciones al cliente — canales como estrategias plug-in

**Estado:** Aceptado e implementado
**Fecha:** 2026-08-30

**Contexto:**

Al iniciar una Obra (`Aprobada` → `EnProceso`) el sistema debe avisar al cliente. Hoy el único canal disponible es correo, pero a futuro se planea agregar Telegram y WhatsApp. El requisito explícito era que agregar un canal nuevo **no debía requerir tocar la lógica de negocio** (`ObraService`) ni ningún tipo compartido cada vez que se sume un canal.

**Decisión:**

Se introduce una capa de notificación desacoplada del canal de entrega, con Strategy + Composite:

1. `INotificationChannel` (`App.Core.Interfaces.Notifications`) es la interfaz de estrategia — una implementación por canal. Expone `ChannelType`, `CanSend(NotificationMessage)` y `SendAsync(...)`.
2. `INotificationService.NotifyAsync(NotificationMessage)` es el orquestador: recibe `IEnumerable<INotificationChannel>` por DI y hace **fan-out** a todos los canales cuyo `CanSend` devuelva `true` para ese mensaje — no elige uno solo con un `switch`.
3. `NotificationMessage` (`App.Core.Models.Notifications`) es el payload agnóstico de canal. Direcciona por canal con `Recipients: IReadOnlyDictionary<NotificationChannelType, string>` (email, chat id, número, etc.) en vez de una propiedad `RecipientX` por canal — así, agregar un canal nunca requiere modificar este contrato, cada canal solo lee su propia entrada del diccionario. También lleva `Attachments` genéricos (`NotificationAttachment`), reusables por cualquier canal que soporte adjuntos.
4. Cada intento de envío (éxito o falla, por canal) se persiste en `not_notificaciones_log` (`NotificationLog`, `App.Models.Notifications`) — historial de auditoría, no un estado de la Obra. La entrega es **best-effort**: una falla de canal solo se loguea (en la tabla y en `ILogger`), nunca revierte ni bloquea la transición de estado que la disparó.
5. El caso de uso concreto (`ObraService.IniciarAsync`) primero completa la transacción de cambio de estado y solo después arma el `NotificationMessage` y llama a `NotifyAsync` — envuelto en su propio try/catch, de modo que un fallo de notificación jamás afecta el resultado de `IniciarAsync`.

**Extender con un canal nuevo (ej. Telegram):**
- Implementar `TelegramNotificationChannel : INotificationChannel`.
- Registrar `services.AddScoped<INotificationChannel, TelegramNotificationChannel>()` en `Program.cs`.
- Nada más cambia: ni `INotificationService`, ni `NotificationMessage`, ni `ObraService`.

**Consecuencias:**

- Positivas:
  - Open/Closed real: agregar un canal es una clase + una línea de DI, sin tocar el core.
  - El historial de notificaciones (`not_notificaciones_log`) es auditable independientemente del estado de negocio de la entidad relacionada (Obra, Cotización, etc. vía `RelatedEntityType`/`RelatedEntityId`).
  - El fan-out (en vez de "elegir un canal") permite que un cliente reciba el mismo evento por varios canales simultáneamente si en el futuro se configuran preferencias múltiples.
- Negativas / a vigilar:
  - `Recipients` es `string`-typed por canal; si un canal necesita más de un dato (p. ej. Telegram podría necesitar chat id + tema/thread), su implementación deberá empaquetar esa información en el string (ej. JSON o un separador) o el contrato deberá revisarse — no se previó una estructura más rica para no sobre-diseñar antes de tener un segundo canal real.
  - No hay UI de administración para consultar `not_notificaciones_log` todavía — solo se persiste. Si se necesita revisar fallos de entrega desde la UI, se debe agregar como trabajo aparte.

**Actualización 2026-08-31 — Telegram descartado como canal de notificación push a clientes:**

Se evaluó implementar `TelegramNotificationChannel` (el `NotificationChannelType.Telegram` ya existía en el enum previendo esto) y se descartó. El Bot API de Telegram no permite que un bot le escriba primero a un usuario que nunca lo contactó — ni por teléfono ni por username; solo puede responder dentro de chats que el usuario inició (deep-link `/start`, modo inline, etc.), confirmado en la documentación oficial (`core.telegram.org/bots/features`) y consistente con cómo operan todos los proveedores del mercado (Manychat, SendPulse, UChat, MessageWhiz — todos automatizan sobre chats ya iniciados por el usuario, ninguno hace push en frío). "Telegram Business" tampoco lo resuelve: automatiza respuestas sobre chats que el cliente ya inició con la cuenta de negocio, no permite iniciar contacto.

Esto es lo opuesto al modelo de WhatsApp Business API, que sí está diseñado para mensajes salientes a números que nunca escribieron primero (vía plantillas pre-aprobadas + opt-in). Por eso Telegram no es viable como canal de notificación transaccional a clientes (ej. "tu Obra inició"), a menos que se acepte un flujo manual donde el cliente le dé `/start` al bot y capture su `chat_id` a mano — considerado pero no implementado por ahora.

`NotificationChannelType.Telegram` se deja en el enum sin implementación de canal, por si en el futuro se usa para un caso distinto (ej. broadcast a un canal/grupo interno, que sí es soportado sin restricción de "quién contacta primero").

**Actualización 2026-09-01 — Telegram adoptado para alertas internas al staff (no a clientes):**

La restricción de "el usuario debe contactar primero" descrita arriba solo bloquea el caso cliente-en-frío. Para **staff interno** sí es viable: cada usuario le da `/start` al bot una sola vez (no es contacto en frío, es gente de la propia organización), y desde ahí el bot puede escribirle. Se implementó `TelegramNotificationChannel : INotificationChannel` (el mismo Strategy de este ADR, sin modificarlo) más una capa de suscripción por usuario y evento encima:

- `NotificationEventType` (`App.Core/Enums/Notifications`) es el catálogo de eventos de negocio suscribibles: `ObraIniciada`, `ObraFinalizada`, `ObraVencida`, `CotizacionAprobada`, `CotizacionRechazada`, `ActividadVencida`. Los dos últimos "Vencida" son placeholders — el sistema no tiene campo de fecha límite ni job de vencidos todavía, así que no se disparan aún.
- `UserNotificationSubscription` (`App.Models/Notifications`) guarda, por usuario, qué eventos quiere recibir en qué canal. `IInternalNotificationDispatcher.DispatchAsync(...)` resuelve los suscriptores de un evento y llama a `INotificationService.NotifyAsync` una vez por usuario — así el fan-out/log de este ADR no se toca, solo se le agrega "a quién avisar" por encima.
- Vinculación de cuenta vía PIN: el usuario genera un código de 6 dígitos en su perfil (`Profile.razor` → tab "Telegram" → `TelegramLinkTab.razor`), se lo escribe al bot, y `TelegramWebhookController` (`api/telegram/webhook`, autenticado por el `X-Telegram-Bot-Api-Secret-Token` que Telegram reenvía, no por sesión de usuario) lo valida y guarda el `chat_id` en `ApplicationUser.TelegramChatId`. Ese webhook es también el punto de extensión pensado para una futura respuesta con IA (hoy cualquier mensaje que no sea un PIN válido recibe una respuesta genérica fija).
- El bot token y la URL pública del webhook (configurable, no hardcodeada — se re-registra con `setWebhook` cada vez que el admin la guarda) viven en `TelegramSettings`, editable en Admin → Settings → tab "Telegram".
- Los 4 eventos con disparador real se conectan en `ObraService` (`NotifyObraIniciadaAsync`, `NotifyObraFinalizadaAsync`) y `CotizacionService` (`NotifyCotizacionAprobadaAsync`, `NotifyCotizacionRechazadaAsync`), siguiendo la misma regla de este ADR: siempre después del commit, en su propio try/catch, sin afectar el `Result` devuelto al llamador.

**Referencias:**
- [`INotificationChannel`](../../../src/App.Core/Interfaces/Notifications/INotificationChannel.cs)
- [`INotificationService`](../../../src/App.Core/Interfaces/Notifications/INotificationService.cs)
- [`NotificationService`](../../../src/App.Services/Notifications/NotificationService.cs) / [`EmailNotificationChannel`](../../../src/App.Services/Notifications/Channels/EmailNotificationChannel.cs)
- `ObraService.IniciarAsync` en `src/App.Services/Obras/ObraService.cs`
