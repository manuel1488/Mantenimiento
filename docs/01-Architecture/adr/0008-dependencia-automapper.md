### ADR-008: Dependencia AutoMapper — Gestión de Licencia y Vulnerabilidad

**Estado:** Aceptado  
**Fecha:** 2026-04-19

---

## Contexto

AutoMapper es la librería de mapeo objeto-objeto usada en toda la capa de servicios (`App.Services`) para convertir entre entidades y DTOs. A partir de la versión 13.0 el autor cambió el modelo de licenciamiento:

| Versión | Licencia | Renovación |
|---------|---------|------------|
| ≤ 12.0.1 | MIT (perpetua) | No requerida |
| ≥ 13.0 | Comercial | Anual (tier gratuito con ≤ 5 devs) |

El tier gratuito de v13+ exige generar y embeber una clave de licencia que expira cada 12 meses. Si la clave no se renueva a tiempo, la aplicación lanza excepciones en runtime.

Adicionalmente, la versión 12.0.1 (MIT) tiene registrada la vulnerabilidad de seguridad **GHSA-rvv3-g6hj-g44x** (CVE-2026-32933, severidad alta, CVSS 7.5):

- **Descripción:** AutoMapper no tiene límite de profundidad de recursión por defecto. Un atacante puede enviar un grafo de objetos con anidación excesiva y provocar un `StackOverflowException` que termina el proceso.
- **Versiones parchadas:** 15.1.1 y 16.1.1 — ambas requieren licencia comercial.
- **Conclusión:** No existe versión de AutoMapper que sea MIT _y_ libre de esta vulnerabilidad.

---

## Evaluación del riesgo

La explotabilidad en este proyecto es **baja** por las siguientes razones:

1. **AutoMapper solo mapea objetos internos.** Los mapeos ocurren entre entidades de la base de datos y DTOs propios. Ningún `Map<>()` recibe estructuras que provengan directamente de input externo sin validar.
2. **Blazor Server no expone endpoints REST genéricos.** Las llamadas llegan a través del circuit de SignalR; el atacante no puede enviar grafos arbitrarios al servidor sin antes autenticarse y pasar por la validación de formularios de Blazor.
3. **Los perfiles de mapeo son planos.** Los `Profile` existentes mapean entidades con un nivel de anidación fijo y conocido en tiempo de compilación.

El riesgo residual es de disponibilidad (crash de la app), no de confidencialidad ni integridad.

---

## Decisión

**Permanecer en AutoMapper 12.0.1 de forma temporal**, con plan de migración a **Mapperly** como solución permanente.

Razones para no actualizar a v13+:
- La dependencia de licencia comercial introduce un riesgo operacional mayor al de la vulnerabilidad: una renovación olvidada detiene producción.
- El tier gratuito no está garantizado a largo plazo por el autor.

Razones para elegir Mapperly como destino:
- Licencia MIT, sin cambios de modelo previsibles.
- Generación en tiempo de compilación (source generators): sin reflexión en runtime, sin vulnerabilidades de este tipo.
- Errores de mapeo detectados en compilación, no en producción.
- Rendimiento superior al basado en reflexión.
- API conceptualmente similar: se reemplazan clases `Profile` por interfaces anotadas con `[Mapper]`.

---

## Plan de migración

La migración a Mapperly no es urgente dado el riesgo bajo evaluado, pero debe realizarse antes de cualquier ampliación significativa de la capa de servicios.

**Alcance estimado:**
1. Agregar `Riok.Mapperly` como dependencia en `App.Services` y `App.Core`.
2. Convertir cada `Profile` de AutoMapper a una interfaz `partial` con `[Mapper]`.
3. Reemplazar las inyecciones de `IMapper` por las interfaces específicas de Mapperly.
4. Eliminar `AutoMapper` y `AutoMapper.Extensions.Microsoft.DependencyInjection` de todos los proyectos.

Los perfiles actuales están en `App.Services/Mappings/`. La migración puede hacerse de forma incremental por dominio (Shop → Admin → Billing).

---

## Consecuencias

**Positivas:**
- Riesgo de licencia eliminado a largo plazo con Mapperly.
- Mapeos validados en compilación reducen bugs silenciosos en producción.

**Negativas:**
- La deuda técnica persiste hasta completar la migración.
- La vulnerabilidad GHSA-rvv3-g6hj-g44x permanece activa en v12.0.1; requiere monitoreo si cambia el modelo de exposición de la aplicación (p. ej. si se agregan endpoints públicos que reciban grafos de objetos).

---

## Referencias

- [GHSA-rvv3-g6hj-g44x — AutoMapper Stack Overflow](https://github.com/advisories/GHSA-rvv3-g6hj-g44x)
- [Mapperly — documentación oficial](https://mapperly.riok.app/)
- [AutoMapper 13.0 license announcement](https://github.com/AutoMapper/AutoMapper/releases/tag/v13.0.0)
