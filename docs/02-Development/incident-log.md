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
