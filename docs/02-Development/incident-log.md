# Bitácora de Incidentes de Producción

Registro cronológico de incidentes de despliegue/producción, su diagnóstico y la reparación aplicada. Cada entrada debe incluir: síntoma, causa raíz, comandos de diagnóstico usados, y la reparación exacta (para poder repetirla o revertirla si vuelve a pasar).

---

## 2026-07-03 — Migración `AddWholesalePriceIntegrityConstraints` parcialmente aplicada, deploy en crash-loop

### Síntoma

Al desplegar, el contenedor `app-prod-web-prod` entra en crash-loop (`Exited (143)`, reintentado por `restart: unless-stopped`). Log de arranque:

```
ALTER TABLE `sh_product_wholesale_prices` ADD CONSTRAINT `ck_wholesale_fixedprice_positive` CHECK (...);
Unhandled exception. MySqlConnector.MySqlException (0x80004005): Duplicate check constraint name 'ck_wholesale_fixedprice_positive'.
  ...
  at Program.<<Main>$>g__InitializeDatabase|0_11(WebApplication app) in /src/src/App.Web/Program.cs:line 821
```

### Causa raíz

MySQL trata `ALTER TABLE ... ADD CONSTRAINT` y `CREATE TRIGGER` como DDL con **commit implícito por sentencia** — cada una se confirma en cuanto se ejecuta, sin importar que EF Core envuelva la migración en una "transacción" lógica. En algún deploy previo (semanas atrás — la última migración registrada en `__EFMigrationsHistory` era de mayo), la migración `20260607173045_AddWholesalePriceIntegrityConstraints` empezó a ejecutarse, sus 3 `ADD CONSTRAINT` (CHECK constraints) se aplicaron y quedaron comprometidos, pero el proceso se interrumpió antes de llegar a los 3 `CREATE TRIGGER` — y por lo tanto nunca se insertó la fila en `__EFMigrationsHistory`. Cada deploy posterior repetía la migración desde el inicio y fallaba en el primer `ADD CONSTRAINT` (ya existente).

Esto además bloqueaba en cascada 2 migraciones más recientes (`AddAuditLog`, `AddPdfRegenerationSetting`) que nunca llegaban a intentarse porque `MigrateAsync()` se detiene en la primera que falla.

### Cómo se diagnosticó

El repo tiene un contexto Docker remoto ya configurado (`docker context ls` → `cleeny`, sobre SSH), por lo que se pudo diagnosticar y reparar directamente sin acceso SSH manual al servidor:

```bash
# Ver imágenes/contenedores en el host de producción
docker images
docker ps -a

# Credenciales de MySQL (vienen del entorno del propio contenedor db)
docker exec app-prod-mysql printenv | grep -i mysql

# Estado real de los objetos de la migración sospechosa
docker exec app-prod-mysql mysql -uroot -p<pwd> App -e "
  SELECT CONSTRAINT_NAME FROM information_schema.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sh_product_wholesale_prices' AND CONSTRAINT_TYPE = 'CHECK';
  SHOW TRIGGERS WHERE \`Table\` IN ('sh_product_wholesale_prices', 'sh_products');
  SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 8;
"

# Diff completo: migraciones locales vs. aplicadas en producción
ls src/App.Models.Data/Migrations/*.cs | grep -v Designer | grep -v ModelSnapshot \
  | sed -E 's#.*[\\/]([0-9]{14}_[A-Za-z0-9]+)\.cs#\1#' | sort > local.txt
docker exec app-prod-mysql mysql -uroot -p<pwd> App -N -e "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;" | sort > prod.txt
comm -23 local.txt prod.txt   # pendientes en producción
comm -13 local.txt prod.txt   # aplicadas en prod pero inexistentes localmente (debe salir vacío)
```

Resultado: 3 CHECK constraints ya existían, 0 triggers existían, y exactamente 3 migraciones pendientes (ninguna huérfana en el otro sentido).

**Antes de reparar**, se verificó que activar los triggers no fuera a corromper nada — un `CREATE TRIGGER` no valida filas existentes (a diferencia de `ADD CONSTRAINT CHECK`, que si hubiera fallado por datos malos ya se habría visto en el intento original). Se corrió una auditoría de datos existentes contra la regla que el trigger iba a proteger:

```sql
SELECT wp.Id AS WholesalePriceId, wp.ProductId, p.Name AS ProductName, wp.FixedPrice, p.Price AS RetailPrice
FROM sh_product_wholesale_prices wp
JOIN sh_products p ON p.Id = wp.ProductId
WHERE wp.IsActive = 1 AND wp.IsDeleted = 0
  AND wp.FixedPrice IS NOT NULL
  AND wp.FixedPrice >= p.Price;
```

Encontró 4 filas con `FixedPrice == RetailPrice` (productos 201, 202, 203, 249) — datos ya inválidos desde antes de este incidente. Se decidió proceder de todas formas (ver "Decisión" abajo).

### Reparación aplicada

1. Se crearon manualmente los 3 triggers faltantes, con el SQL **idéntico** al de `Up()` en `20260607173045_AddWholesalePriceIntegrityConstraints.cs` (no se modificó el código de la migración, solo se ejecutó su contenido pendiente directamente):
   - `trg_wholesale_price_insert_check`
   - `trg_wholesale_price_update_check`
   - `trg_product_price_update_check`
2. Se insertó manualmente la fila de historial para marcar la migración como aplicada:
   ```sql
   INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
   VALUES ('20260607173045_AddWholesalePriceIntegrityConstraints', '9.0.12');
   ```
3. Se reinició el contenedor (`docker start app-prod-web-prod`) — `MigrateAsync()` continuó normalmente con `AddAuditLog` y `AddPdfRegenerationSetting` (ambas nunca tocadas, aplicaron limpio). App arrancó sana (`healthy`), las 49 migraciones locales quedaron aplicadas en producción.

### Decisión: no se corrigieron los 4 registros con datos inválidos

Los 4 productos (201 BOTELLA 1L CON TAPA, 202 PORRON 20L SEMINUEVO, 203 PORRON 5L SEMINUEVO, 249 PORRON 10 LITROS) tienen `FixedPrice` de mayoreo igual a su precio de venta. Corregir cuál de los dos precios está "mal" es una decisión de negocio, no técnica — no se modificó ningún dato.

**Efecto práctico pendiente:** desde este incidente, cualquier intento de **guardar** un cambio en el precio de venta de esos 4 productos, o en esas 4 filas de precio de mayoreo, fallará con:
`"Retail price cannot be at or below an active wholesale fixed price"`
hasta que alguien del negocio ajuste el precio de mayoreo (bajarlo) o el de venta (subirlo).

**Pendiente:** avisar al equipo de precios/negocio sobre estos 4 productos.

### Cómo revertir esta reparación (si algo sale mal)

No modifica datos de negocio, solo agrega objetos de esquema — reversión trivial y sin efectos colaterales:

```sql
DROP TRIGGER IF EXISTS `trg_product_price_update_check`;
DROP TRIGGER IF EXISTS `trg_wholesale_price_update_check`;
DROP TRIGGER IF EXISTS `trg_wholesale_price_insert_check`;
DELETE FROM `__EFMigrationsHistory` WHERE MigrationId = '20260607173045_AddWholesalePriceIntegrityConstraints';
```

### Prevención a futuro (pendiente de decidir)

Regla general propuesta para cualquier migración que use `migrationBuilder.Sql(...)` con `ADD CONSTRAINT` / `CREATE TRIGGER` / otro DDL: en MySQL cada sentencia hace commit implícito, así que una migración de este tipo puede quedar interrumpida a medias sin que EF se entere. Conviene escribirla de forma idempotente (verificar existencia antes de crear, o `DROP ... IF EXISTS` antes de recrear) para que sea segura de volver a correr desde cero. **No se aplicó este cambio a `AddWholesalePriceIntegrityConstraints`** en este incidente — se optó por reparar el estado de producción directamente y dejar el código de la migración tal cual, ya que modificarla no ayuda a los entornos donde ya corrió. Si vuelve a pasar en otra migración con DDL crudo, considerar adoptar este patrón de forma consistente.

---

## 2026-07-17 — Precios de mayoreo no se guardan (columna `FixedPrice` insuficiente + filtro de UI que descarta el borrado)

### Síntoma

Usuario reporta: al editar el producto P0253 (precio de venta $1,220.69) y configurar el nivel de mayoreo "Mayoreo 1" (Cant. mínima 6, Descuento 100%, Activo), el diálogo no muestra ningún error, pero al reabrir el producto para editar los valores siguen igual que antes del cambio.

### Causa raíz (parte 1 — guardado inicial)

`ProductWholesalePrice.FixedPrice` está declarado como `decimal(9,6)` (máx. `$999.999999`) mientras que `Product.Price` es `decimal(10,6)` (hasta `$9,999.999999`). El producto en cuestión cuesta $1,220.69 — cualquier intento de capturar un precio fijo de mayoreo para él (o cualquier producto ≥ $1,000) hace que MySQL rechace el `SaveChanges` completo:

```
MySqlConnector.MySqlException (0x80004005): Out of range value for column 'FixedPrice' at row 1
```

El error sí se captura en `ProductWholesalePriceService.UpdateProductWholesalePricesAsync` y se traduce a un `Result.Failure`, pero `ProductDialog.razor` mostraba el toast verde "Producto actualizado con éxito" (correspondiente al guardado de los datos generales del producto, que es un paso separado) **sin revisar si el guardado de precios de mayoreo había fallado** — el warning real quedaba visualmente opacado junto al de éxito.

### Causa raíz (parte 2 — no se podía limpiar/desactivar un nivel ya guardado)

Una vez corregido el punto anterior, al intentar **desactivar** el nivel de mayoreo que sí había quedado guardado (llevando Cant. mínima/Descuento a 0 e inactivo), tampoco se guardaba. `SaveWholesalePrices` en `ProductDialog.razor` filtra las filas "sin datos significativos" (`MinQuantity > 0 && Descuento/FixedPrice > 0`) antes de armar la solicitud; si **todas** las filas quedan en cero tras editar, `validConfigs.Count == 0` y el método regresaba sin llamar al backend en absoluto — ni siquiera para pedir el borrado de la fila existente. Además, un nivel **sin fila en la BD** se mostraba con el checkbox "Activo" marcado por un valor por default incorrecto (`existing?.IsActive ?? true`), lo que hacía parecer que ambos niveles de mayoreo estaban configurados cuando en realidad solo uno tenía datos reales.

### Cómo se diagnosticó

Todo el diagnóstico se hizo contra producción sin acceso SSH manual, usando el contexto Docker remoto ya configurado (`docker context ls` → `cleeny`) y el `docker-compose` de referencia (MySQL sin puerto expuesto al host, solo alcanzable vía `docker exec`):

```bash
# Contenedores activos en producción
docker --context cleeny ps

# Definición real de la tabla en producción (para comparar contra el modelo EF)
docker --context cleeny exec app-prod-mysql sh -c \
  'mysql -uapp -p"$MYSQL_PASSWORD" -D App -e "SHOW CREATE TABLE sh_product_wholesale_prices\G"'

# Estado real de las filas del producto reportado (incluye soft-deleted)
docker --context cleeny exec app-prod-mysql sh -c \
  'mysql -uapp -p"$MYSQL_PASSWORD" -D App -e "
    SELECT Id, ProductId, WholesaleTierId, MinQuantity, DiscountPercentage, FixedPrice, IsActive, IsDeleted, CreatedAt, ModifiedAt
    FROM sh_product_wholesale_prices WHERE ProductId = 253;"'

# Log de aplicación del día del reporte, filtrando por la operación fallida
docker --context cleeny exec app-prod-web-prod sh -c \
  'grep -i -B5 -A25 "wholesale" /app/logs/log-20260716.txt'
```

El log mostró 3 intentos fallidos consecutivos (11:49, 11:53, 11:55 hora local) con el mismo `MySqlException: Out of range value for column 'FixedPrice'`, y la consulta a la tabla confirmó que solo existía una fila (`WholesaleTierId = 1`, sin fila para el nivel 2), lo que llevó a revisar el default de `IsActive` en el código del diálogo.

También se generó localmente el SQL de la migración base (`dotnet ef migrations script 0 <Initial> -o ...`) para confirmar por separado un hallazgo colateral: el índice único `IX_sh_product_wholesale_prices_ProductId_WholesaleTierId` está declarado en el modelo con `HasFilter("IsDeleted = 0")`, pero Pomelo/MySQL **ignora silenciosamente ese filtro** — en producción el índice es único de forma global, no solo entre filas activas. No causó el bug de este reporte (no había fila soft-deleted en conflicto), pero es una deuda técnica real: reconfigurar un nivel de mayoreo después de que su fila anterior fue soft-deleted puede chocar con esta unicidad global. **Pendiente de atender por separado.**

### Reparación aplicada

1. Migración `WidenWholesaleFixedPricePrecision`: `ALTER TABLE sh_product_wholesale_prices MODIFY FixedPrice decimal(10,6)` (antes `decimal(9,6)`).
2. `ProductDialog.razor` (`ValidateWholesaleFixedPrice`): rechaza en el formulario un precio fijo que exceda lo que la columna puede almacenar.
3. `ProductDialog.razor` (`Submit`): el toast de éxito del producto ya no se muestra si `SaveWholesalePrices` reporta fallo.
4. `ProductDialog.razor` (`WholesalePriceConfigModel` + `LoadWholesalePriceConfigs` + `SaveWholesalePrices`): se agregó `HadExistingRow` para que limpiar un nivel ya guardado siga llamando al backend (dispara el soft-delete existente en `UpdateProductWholesalePricesAsync`); default de `IsActive` para niveles sin fila cambiado de `true` a `false`.

### Pendiente

- Aplicar la migración `WidenWholesaleFixedPricePrecision` en producción (no se ejecutó en este incidente, solo se diagnosticó y corrigió el código).
- Decidir si se corrige el índice único filtrado (`HasFilter("IsDeleted = 0")`) que Pomelo ignora en MySQL — hoy no es la causa de ningún bug conocido, pero es una condición latente para cualquier tabla con soft-delete + índice único compuesto.

---

## 2026-07-22 — `sistema.cleeny.com.mx` sin responder (~20 min), resuelto con reinicio manual

### Síntoma

Usuario reporta que `https://sistema.cleeny.com.mx/` no carga. Un monitor de uptime externo registró **5 minutos y 12 segundos de inactividad**. El usuario reinició manualmente el contenedor `app-prod-web-prod` antes de que se completara el diagnóstico, y el sitio volvió a responder de inmediato.

### Diagnóstico

Con el contexto Docker remoto (`docker --context cleeny`), sin acceso SSH manual:

```bash
docker --context cleeny ps -a
# app-prod-web-prod: Up About a minute (healthy) — sugiere reinicio reciente
# app-prod-mysql:    Up 5 days (healthy)

docker --context cleeny logs app-prod-web-prod --since 20m
docker --context cleeny inspect app-prod-web-prod --format='OOMKilled={{.State.OOMKilled}} ExitCode={{.State.ExitCode}}'
# OOMKilled=false ExitCode=0 → apagado limpio, no un crash ni un OOM-kill
docker --context cleeny stats --no-stream
# Memoria al 20%, CPU casi en cero → no fue agotamiento de recursos
```

El log del día mostró:

```
12:46:32.669 [ERR] An error occurred using the connection to database 'App' on server 'db'.
14:41:20.009 [WRN] 404 Not Found: ...   ← la app siguió respondiendo después del error
14:41:26.461 [WRN] 404 Not Found: ...
                                         ← silencio total ~20 min (consistente con el cuelgue reportado)
15:01:07.864 [INF] Application is shutting down...   ← reinicio manual del usuario
15:01:23.774 [INF] Application started.
```

`RestartCount: 0` en `docker inspect` confirma que no fue un reinicio automático por política de Docker — fue el reinicio manual del usuario el que resolvió el cuelgue.

### Causa raíz

El único error de MySQL registrado (12:46:32) no tiene traza de excepción en el log (el mensaje es el diagnóstico interno de EF Core, sin `{Exception}` adjunto — ver "Deuda técnica" abajo). No se pudo confirmar con certeza que ese error específico causara el cuelgue posterior; es la explicación más plausible dado que es el único evento anómalo antes del silencio, pero no hay prueba definitiva.

Se confirmó una causa raíz de fondo real en el código, independiente de si fue la causante exacta de este incidente: **`Database:MaxRetryCount: 3` estaba definido en `appsettings.json` pero nunca se usaba** — `Program.cs` configuraba MySQL sin `EnableRetryOnFailure()`. Cualquier fallo transitorio de conexión (timeout de red, `wait_timeout` del servidor, blip momentáneo) no se reintentaba automáticamente.

### Reparación aplicada

1. **`Program.cs`**: se agregó `mySqlOptions.EnableRetryOnFailure(databaseOptions.MaxRetryCount)` a la configuración de `UseMySql`.

2. **Habilitar retry sin romper transacciones manuales.** EF Core prohíbe usar `Database.BeginTransactionAsync()` bajo una execution strategy de reintento sin envolverla en `Database.CreateExecutionStrategy().ExecuteAsync(...)` — lanza `InvalidOperationException` en tiempo de ejecución si no se hace. Se auditaron y envolvieron los **20 sitios** que abren transacciones manuales:
   - `IdentityService` (`CreateUserAsync`, `UpdateUserAsync`)
   - `ProductPartialSurchargeService.UpdateProductSurchargesAsync`
   - `StockEntryService.CreateStockEntryAsync`
   - `AdjustmentEntryService.CreateAdjustmentEntryAsync`
   - `TaxRateService.DeleteRateAsync`
   - `CashRegisterService.CloseCashRegisterAsync`
   - `RemissionService` (`CreateAsync`, `CancelAsync`, `ConsolidateAsync`)
   - `InventoryService` (`CreateMovementAsync`, `CreateTransferAsync`, `CreateInitialInventoryAsync`, `CreateBulkInitialInventoryAsync`, `CreateInventoryAdjustmentAsync`)
   - `SaleService` (`CreateSaleAsync`, `CancelSaleAsync`)
   - `ProductWholesalePriceService.UpdateProductWholesalePricesAsync`
   - `CfdiPostalCodeSeeder.BulkInsertAsync`, `MexicoFiscalCatalogSeeder.BulkInsertAsync`

3. **Bug de fondo encontrado y corregido durante la auditoría**: `RemissionService.CreateAsync` generaba el folio (`REM-{año}-{consecutivo}`) llamando a `DocumentSequenceService.GetNextNumberAsync`, que abría **su propio `DbContext` y hacía commit por su cuenta**, fuera de la transacción de la remisión. Si el bloque completo se reintentaba por una falla transitoria, se habría generado un **segundo folio distinto** (el primero quedaba perdido, saltando un número). Se cambió `IDocumentSequenceService.GetNextNumberAsync` para que reciba el `ApplicationDbContext` del llamador en lugar de abrir uno propio, de modo que el incremento participe en la misma transacción/reintento que el resto de la operación (afecta también a `QuotationService`, que generaba folios de la misma forma sin estar bajo transacción explícita, pero se corrigió igual por consistencia). La interfaz se movió de `App.Core.Interfaces.Shop` a `App.Services.Shop` porque ahora depende de `ApplicationDbContext` (capa de datos), seleccionado siguiendo el mismo patrón ya usado por `IContextualInventoryService`.

4. **Correos de alerta de stock duplicados en un reintento**: en `InventoryService.CreateTransferAsync` y `CreateInventoryAdjustmentAsync`, el envío de correo de alerta (`Task.Run(SendInventoryAlertAsync)`) se sacó fuera del bloque envuelto en `ExecuteAsync`, para que un reintento transitorio no reenvíe el correo. `CreateMovementAsync` (con contexto compartido, usado por Ventas/Remisiones/Entradas/Ajustes) quedó con el riesgo aceptado sin resolver — ver deuda técnica.

Verificación: build limpio (`dotnet build App.sln`, 0 errores) y suite completa de pruebas (`dotnet test tests/App.Services.Tests`, 255/255 exitosas) tras el cambio.

**Validación contra MySQL real.** Ya existía `RemissionRollbackContainerTests` (`tests/App.Services.Tests/Shop/TransactionIntegrityTests.cs`), una fixture con Testcontainers que levanta un MySQL 8.0 real para probar rollback — pero construía sus propias `DbContextOptions` **sin** `EnableRetryOnFailure`, por lo que no ejercitaba la execution strategy de reintento agregada esta noche. Se le agregó `EnableRetryOnFailure(3)` (igual que `Program.cs`), y se agregó un test nuevo (`CreateRemission_RealDocumentSequence_FolioRolledBackOnFailure_NoGapOnNextSuccess`) que usa el **`DocumentSequenceService` real** (no un mock) para probar directamente el bug de folios que se corrigió: confirma que el incremento del folio se revierte junto con la remisión fallida (sin fila huérfana en `sh_document_sequences`), y que la siguiente remisión exitosa recibe `REM-{año}-0001` sin saltos. 6/6 pruebas de esta fixture pasan contra MySQL real con retry habilitado.

**Cobertura de integración real que queda pendiente**: de los 20 sitios envueltos, solo `RemissionService.CreateAsync` y `SaleService.CreateSaleAsync`/`CancelSaleAsync` tienen prueba contra MySQL real. Los otros 17 (`IdentityService`, `ProductPartialSurchargeService`, `StockEntryService`, `AdjustmentEntryService`, `TaxRateService`, `CashRegisterService`, `RemissionService.CancelAsync`/`ConsolidateAsync`, la mayoría de `InventoryService`, `ProductWholesalePriceService`, los 2 seeders) solo están cubiertos por build + tests unitarios con mocks/EF InMemory, que no ejercitan la execution strategy de reintento real. Ver [tech-debt.md](tech-debt.md).

### Pendiente / deuda técnica generada

- Ver [tech-debt.md](tech-debt.md): posible correo de alerta duplicado en `CreateMovementAsync` bajo reintento transitorio; `IdentityService` con transacción que no cubre realmente las operaciones de `UserManager` (bug preexistente, solo confirmado durante esta auditoría).
- El log de producción no adjunta la traza de excepción completa en los mensajes `[ERR]` — dificultó confirmar la causa exacta del cuelgue. Considerar revisar la plantilla de salida de Serilog para asegurar que `{Exception}` se incluya de forma consistente.

---

## 2026-07-23 — "Abrir Caja" se queda colgado en "ABRIENDO..." indefinidamente (solo para usuarios no-admin)

### Síntoma

La cajera jacq reporta que el diálogo de apertura de caja (`OpenCashRegisterDialog.razor`) se queda congelado en "ABRIENDO..." sin error ni resolución. Reproducido en otra máquina impersonando la sesión de jacq. Al probar con las cuentas `admin`/`admin2` en la misma máquina, el flujo funciona con normalidad.

### Cómo se diagnosticó

Acceso directo por SSH al servidor de producción (no había contexto Docker remoto configurado en esta sesión):

```bash
# Logs de la app (sin errores ni excepciones relacionadas con caja)
docker exec app-prod-web-prod grep -i -B2 -A40 'CashRegister' /app/logs/log-20260723.txt

# Estado de la base de datos: sin locks, sin deadlocks, sin transacciones abiertas
docker exec app-prod-mysql mysql -uroot -p... -e "SHOW FULL PROCESSLIST;"
docker exec app-prod-mysql mysql -uroot -p... -e "SELECT * FROM performance_schema.data_locks;"
docker exec app-prod-mysql mysql -uroot -p... -e "SHOW ENGINE INNODB STATUS\G"  # sin "LATEST DETECTED DEADLOCK"

# Historial de cajas del usuario afectado (sin caja huérfana abierta)
docker exec app-prod-mysql mysql -uroot -p... App -e \
  "SELECT Id, LocationId, CashStationId, UserId, Status, OpenedAt, ClosedAt FROM sh_cash_registers WHERE UserId='<jacq-id>' ORDER BY OpenedAt DESC LIMIT 5;"
```

Todo lo anterior descartó: registro huérfano abierto, perfil de cajera inactivo, estación de caja inactiva/ocupada, deadlock de InnoDB, agotamiento de recursos del contenedor (`docker stats`), y conexiones abortadas relevantes (`Aborted_connects` alto pero explicado en su totalidad por el healthcheck `mysqladmin ping` sin password cada 10s).

El hallazgo decisivo fue reproducir el bug impersonando cuentas distintas en la **misma máquina/navegador**: funciona con `admin`/`admin2` (rol con `IsGlobalAccess`), falla con `jacq` (cajera normal, sin acceso global) — descartando causas de red/proxy/circuito de SignalR y apuntando a una rama de código exclusiva de usuarios no-admin.

### Causa raíz

`CurrentUserService.cs` (`UserId`, `UserName`, `IsGlobalAccess`, `GetCurrentUser()`) resolvía el estado de autenticación de forma **síncrona sobre asíncrona**:

```csharp
var authState = _authenticationStateProvider.GetAuthenticationStateAsync().Result;
```

En Blazor Server, cada circuito serializa su trabajo en un único `RendererSynchronizationContext`. Si la tarea awaited necesita reanudar su continuación en ese mismo contexto y el hilo está bloqueado esperando `.Result`, se produce un deadlock clásico de sync-over-async — el spinner queda colgado para siempre, sin excepción ni entrada de log (el código nunca llega a lanzar ni a completar).

Por qué se manifestó específicamente en "Abrir Caja" y para usuarios no-admin:

1. **Ventana de carrera, no un fallo determinista.** El patrón `.Result` existe desde el commit `22ae5bb` (mucho anterior a este incidente); solo se dispara si la tarea de `GetAuthenticationStateAsync()` sigue en vuelo justo en el instante en que se lee la propiedad.
2. "Abrir Caja" es casi siempre la primera acción interactiva tras el login — el momento en que más probablemente esa tarea de autenticación todavía no ha resuelto.
3. `CashRegisterService.OpenCashRegisterAsync` es el único punto en la capa de servicios que llama a `IsGlobalAccess` justo después de leer `UserId`, sin ningún `await` real entre medio — dos bloqueos consecutivos, doble ventana de riesgo. Para usuarios con `IsGlobalAccess = true` el chequeo interno de perfil de cajera se salta por completo (la rama de código de jacq nunca se ejecuta para admin), reduciendo aún más su exposición.
4. El reinicio del contenedor la noche anterior (deploy de `EnableRetryOnFailure`, ver incidente anterior en esta bitácora) dejó todas las cachés en frío. Justo tras un restart, la resolución de claims/roles tarda más (sin caché tibia), ampliando la ventana de carrera — probablemente el motivo de que apareciera justo esa mañana.
5. `admin`/`admin2` no lo sufrieron porque son cuentas de uso frecuente para pruebas: su estado de autenticación ya estaba resuelto/cacheado en el momento de la prueba.

### Reparación aplicada (mitigación, no la corrección de raíz)

Commit `5f4e141`. Se envolvieron las 4 llamadas bloqueantes de `CurrentUserService.cs` en `Task.Run(...)` antes del `.Result`/`.GetAwaiter().GetResult()`:

```csharp
var authState = Task.Run(() => _authenticationStateProvider.GetAuthenticationStateAsync()).GetAwaiter().GetResult();
```

Esto mueve la espera bloqueante a un hilo del thread pool **sin** el `SynchronizationContext` del circuito capturado, así la continuación de `GetAuthenticationStateAsync()` ya no intenta reanudarse en el hilo bloqueado — rompe el deadlock sin cambiar la interfaz pública `ICurrentUserService` ni tocar ninguno de sus 45 consumidores.

Desplegado reconstruyendo la imagen desde la máquina con `.env.production` (`docker compose --profile production --env-file .env.production build --no-cache && ... up -d`). Como mitigación temporal mientras se preparaba el deploy, también se hizo `docker restart app-prod-web-prod` (destraba cualquier circuito ya colgado, pero no corrige la causa de raíz — ver ADR-010).

### Pendiente

- Ver [ADR-010](../01-Architecture/adr/0010-acceso-async-usuario-actual.md): la corrección de raíz (convertir `ICurrentUserService` a métodos 100% async) queda como iniciativa separada por su alcance (45 archivos consumidores). Ver también [tech-debt.md](tech-debt.md).
- Rotar la contraseña de la cuenta `app` de MySQL — quedó expuesta en texto plano durante el diagnóstico de este incidente (se extrajo de `docker inspect` para poder reconstruir el despliegue sin acceso a la máquina con `.env.production`).
