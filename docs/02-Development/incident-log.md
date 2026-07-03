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
