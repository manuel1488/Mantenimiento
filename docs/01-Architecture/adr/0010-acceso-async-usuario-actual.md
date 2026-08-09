### ADR-010: Acceso al usuario actual en Blazor Server — prohibido sync-over-async

**Estado:** Aceptado e implementado
**Fecha:** 2026-07-23

**Contexto:**

`CurrentUserService` (implementación de `ICurrentUserService`, ver [ADR-005](0005-sistema-autenticacion.md)) expone `UserId`, `UserName`, `IsGlobalAccess` y `FullName` como **propiedades síncronas**, aunque internamente dependen de operaciones asíncronas (`AuthenticationStateProvider.GetAuthenticationStateAsync()`, `UserManager.FindByIdAsync()`). Para conciliar esa asincronía con una API síncrona, el código bloqueaba con `.Result`:

```csharp
var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
```

Este patrón (sync-over-async) causó un deadlock real en producción el 2026-07-23: en Blazor Server, cada circuito serializa su trabajo en un único `RendererSynchronizationContext`. Si la tarea asíncrona interna aún no había resuelto en el instante en que se leía la propiedad, y su continuación necesitaba reanudarse en ese mismo contexto, el hilo bloqueado por `.Result` nunca podía liberarse para atenderla — deadlock permanente, sin excepción ni entrada de log.

El bug era preexistente (desde el commit `22ae5bb`) pero probabilístico: solo se manifestaba si la tarea seguía en vuelo en el momento exacto de la lectura — algo mucho más probable justo después del login (cuando el estado de autenticación todavía se está resolviendo) o justo después de un reinicio del contenedor (cachés en frío). Por eso pasó desapercibido tanto tiempo y apareció de forma aparentemente aleatoria, afectando primero a un flujo específico (apertura de caja) y a usuarios no-admin.

**Decisión:**

1. **Prohibido usar `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` directamente sobre `AuthenticationStateProvider.GetAuthenticationStateAsync()`, `UserManager<T>` u otra tarea cuya continuación pueda necesitar el `SynchronizationContext` de un circuito Blazor Server.** Si una API síncrona es indispensable (p. ej. por compatibilidad con una interfaz existente), la espera bloqueante debe ejecutarse en un hilo del thread pool sin el contexto capturado:
   ```csharp
   var authState = Task.Run(() => _authenticationStateProvider.GetAuthenticationStateAsync()).GetAwaiter().GetResult();
   ```
   Esto es una **mitigación aceptada**, no el patrón preferido — evita el deadlock, pero sigue bloqueando un hilo y no debe usarse en código nuevo.

2. **Código nuevo que necesite el usuario actual debe usar las APIs async.** `ICurrentUserService` expone `UserId`, `UserName` y `FullName` únicamente como métodos `Task<...>` (`GetUserIdAsync()`, `GetUserNameAsync()`, `GetFullNameAsync()`) — nunca como propiedades síncronas. Ya no queda ningún `.Result`/`.GetAwaiter().GetResult()`/`Task.Run(...)` en `CurrentUserService.cs` — la mitigación del punto 1 queda como referencia para código nuevo que no pueda evitar una API síncrona, no como el estado actual de este servicio.

3. **Cualquier servicio o componente que dependa de `IAuthenticationStateProvider`/`ICurrentUserService` dentro de un manejador de evento de Blazor Server debe asumir que el estado de autenticación puede no estar resuelto todavía** — especialmente en las primeras interacciones tras el login o tras un reinicio de la app. No asumir que la lectura es "instantánea porque ya se resolvió antes".

**Consecuencias:**

- Positivas:
  - Elimina por completo la clase de bug (deadlock silencioso, sin log) que causó el incidente del 2026-07-23 — no solo la mitiga, la erradica: no queda ningún bloqueo de hilo en el camino de `ICurrentUserService`.
  - Los 45 consumidores ahora reflejan honestamente que leer el usuario actual es una operación async — más fácil de razonar para quien lea el código nuevo.
- Negativas:
  - Refactor de 60 archivos en una sola sesión, inmediatamente después de un incidente en producción — mayor superficie de riesgo de regresión que la mitigación mínima. Mitigado con: build limpio (0 errores) y suite de tests (249/255 — los 6 fallos restantes son pruebas de integración con Testcontainers que requieren Docker, no relacionadas con este cambio) antes de hacer commit.
  - Cualquier código nuevo que inyecte `AuthenticationStateProvider` directamente (en vez de pasar por `ICurrentUserService`) y copie el patrón `.Result` puede reintroducir el mismo bug — este ADR es la referencia a citar en code review si eso ocurre.

**Referencias:**
- [ADR-005: Sistema de Autenticación](0005-sistema-autenticacion.md)
- [ASP.NET Core Blazor Server — evitar bloqueo de hilos](https://learn.microsoft.com/aspnet/core/blazor/performance#avoid-thread-blocking-calls)
