### ADR-010: Acceso al usuario actual en Blazor Server — prohibido sync-over-async

**Estado:** Aceptado (mitigación aplicada, refactor de raíz pendiente)
**Fecha:** 2026-07-23

**Contexto:**

`CurrentUserService` (implementación de `ICurrentUserService`, ver [ADR-005](0005-sistema-autenticacion.md)) expone `UserId`, `UserName`, `IsGlobalAccess` y `FullName` como **propiedades síncronas**, aunque internamente dependen de operaciones asíncronas (`AuthenticationStateProvider.GetAuthenticationStateAsync()`, `UserManager.FindByIdAsync()`). Para conciliar esa asincronía con una API síncrona, el código bloqueaba con `.Result`:

```csharp
var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
```

Este patrón (sync-over-async) causó un deadlock real en producción el 2026-07-23 (ver [incident-log.md](../../02-Development/incident-log.md)): en Blazor Server, cada circuito serializa su trabajo en un único `RendererSynchronizationContext`. Si la tarea asíncrona interna aún no había resuelto en el instante en que se leía la propiedad, y su continuación necesitaba reanudarse en ese mismo contexto, el hilo bloqueado por `.Result` nunca podía liberarse para atenderla — deadlock permanente, sin excepción ni entrada de log.

El bug era preexistente (desde el commit `22ae5bb`) pero probabilístico: solo se manifestaba si la tarea seguía en vuelo en el momento exacto de la lectura — algo mucho más probable justo después del login (cuando el estado de autenticación todavía se está resolviendo) o justo después de un reinicio del contenedor (cachés en frío). Por eso pasó desapercibido tanto tiempo y apareció de forma aparentemente aleatoria, afectando primero a un flujo específico (apertura de caja) y a usuarios no-admin.

**Decisión:**

1. **Prohibido usar `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` directamente sobre `AuthenticationStateProvider.GetAuthenticationStateAsync()`, `UserManager<T>` u otra tarea cuya continuación pueda necesitar el `SynchronizationContext` de un circuito Blazor Server.** Si una API síncrona es indispensable (p. ej. por compatibilidad con una interfaz existente), la espera bloqueante debe ejecutarse en un hilo del thread pool sin el contexto capturado:
   ```csharp
   var authState = Task.Run(() => _authenticationStateProvider.GetAuthenticationStateAsync()).GetAwaiter().GetResult();
   ```
   Esto es una **mitigación aceptada**, no el patrón preferido — evita el deadlock, pero sigue bloqueando un hilo y no debe usarse en código nuevo.

2. **Código nuevo que necesite el usuario actual debe usar (o exponer) APIs async.** `ICurrentUserService` debe migrar `UserId`, `UserName`, `IsGlobalAccess` y `FullName` de propiedades síncronas a métodos `Task<...>` (p. ej. `Task<string> GetUserIdAsync()`). Esto es la corrección de raíz, pendiente como refactor separado por su alcance (45 archivos consumidores en `App.Services` y `App.Web` — ver [tech-debt.md](../../02-Development/tech-debt.md)). Mientras no se complete, todo el código existente sigue protegido por la mitigación del punto 1.

3. **Cualquier servicio o componente que dependa de `IAuthenticationStateProvider`/`ICurrentUserService` dentro de un manejador de evento de Blazor Server debe asumir que el estado de autenticación puede no estar resuelto todavía** — especialmente en las primeras interacciones tras el login o tras un reinicio de la app. No asumir que la lectura es "instantánea porque ya se resolvió antes".

**Consecuencias:**

- Positivas:
  - Elimina la clase de bug (deadlock silencioso, sin log) que causó el incidente del 2026-07-23.
  - No requiere tocar los 45 consumidores actuales de inmediato — la mitigación es interna a `CurrentUserService`.
- Negativas:
  - La mitigación (`Task.Run`) sigue bloqueando un hilo del thread pool por cada acceso a estas propiedades — no es gratis, solo ya no puede colgar el circuito. El costo real de no completar el refactor async es deuda técnica de rendimiento, no de correctitud.
  - Mientras el refactor a async no se haga, cualquier código nuevo que copie el patrón `.Result` sin pasar por `CurrentUserService` (p. ej. un componente que inyecte `AuthenticationStateProvider` directamente) puede reintroducir el mismo bug — este ADR es la referencia a citar en code review si eso ocurre.

**Referencias:**
- [Bitácora de incidentes — 2026-07-23](../../02-Development/incident-log.md)
- [Deuda técnica — ICurrentUserService](../../02-Development/tech-debt.md)
- [ADR-005: Sistema de Autenticación](0005-sistema-autenticacion.md)
- [ASP.NET Core Blazor Server — evitar bloqueo de hilos](https://learn.microsoft.com/aspnet/core/blazor/performance#avoid-thread-blocking-calls)
