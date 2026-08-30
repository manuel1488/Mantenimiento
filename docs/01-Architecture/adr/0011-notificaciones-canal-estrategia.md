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

**Referencias:**
- [`INotificationChannel`](../../../src/App.Core/Interfaces/Notifications/INotificationChannel.cs)
- [`INotificationService`](../../../src/App.Core/Interfaces/Notifications/INotificationService.cs)
- [`NotificationService`](../../../src/App.Services/Notifications/NotificationService.cs) / [`EmailNotificationChannel`](../../../src/App.Services/Notifications/Channels/EmailNotificationChannel.cs)
- `ObraService.IniciarAsync` en `src/App.Services/Obras/ObraService.cs`
