# Database Tools

Scripts de mantenimiento y administración de la base de datos Cleeny.

## Scripts disponibles

### `reset-sales.sql`

Elimina **todas las ventas** y todos los registros relacionados:

| Tabla eliminada | Razón |
|----------------|-------|
| `mx_invoice_files` | FK a facturas (CASCADE) |
| `mx_invoices` | FK a ventas (RESTRICT → va antes que ventas) |
| `sh_inventory_movements` (tipo `SALE`) | Movimientos generados por ventas |
| `sh_sales` | Tabla principal de ventas |
| `sh_sale_details` | FK a ventas (CASCADE) |
| `sh_sale_payments` | FK a ventas (CASCADE) |
| `sh_cash_register_denominations` | FK a cajas (CASCADE) |
| `sh_cash_register_movements` | FK a cajas |
| `sh_cash_registers` | Sesiones de caja |

**Tablas NO afectadas:** productos, clientes, ubicaciones, proveedores, entradas de stock, ajustes de inventario, cotizaciones, configuración, usuarios.

> **Nota:** Los saldos de `sh_inventory` **no se recalculan** automáticamente. Si necesitas resetearlos también, hazlo manualmente después.

---

### `reset-product-catalog.sql`

Elimina **todos los productos** y todos los registros relacionados:

| Tabla eliminada | Razón |
|----------------|-------|
| `mx_invoice_files` | FK a facturas (CASCADE) |
| `mx_invoices` | FK a ventas (RESTRICT → va antes que ventas) |
| `sh_sales` | FK a productos vía detalles (RESTRICT) |
| `sh_sale_details` | FK a ventas (CASCADE) |
| `sh_sale_payments` | FK a ventas (CASCADE) |
| `sh_quotations` | FK a productos vía detalles (RESTRICT) |
| `sh_quotation_details` | FK a productos (RESTRICT) |
| `sh_bulk_label_jobs` | FK a productos (RESTRICT) |
| `sh_adjustment_entry_items` | FK a productos (RESTRICT) |
| `sh_adjustment_entries` | Limpieza de huérfanos |
| `sh_stock_entry_items` | FK a productos (RESTRICT) |
| `sh_stock_entries` | Limpieza de huérfanos |
| `sh_inventory_movements` | FK a productos (RESTRICT) |
| `sh_inventory` | FK a productos (RESTRICT) |
| `sh_product_images` | FK a productos (CASCADE) |
| `sh_product_wholesale_prices` | FK a productos (CASCADE) |
| `sh_product_partial_surcharges` | FK a productos (CASCADE) |
| `sh_products` | Tabla principal |
| `sh_partial_sale_fractions` | Datos semilla del catálogo |

**Tablas NO afectadas:** clientes, ubicaciones, proveedores, métodos de pago, cajas registradoras, usuarios, configuración.

---

### `inventory-reconciliation.sql`

Script de **solo lectura** que verifica la integridad entre `sh_inventory` y `sh_inventory_movements`. Útil después de incidentes transaccionales o antes/después de migraciones.

Ejecuta 3 verificaciones en secuencia:

| Parte | Qué verifica | Resultado esperado |
|-------|-------------|-------------------|
| 1 | Saldo actual vs `NewBalance` del último movimiento | 0 filas |
| 2 | Cadena de movimientos: `PreviousBalance[n]` = `NewBalance[n-1]` | 0 filas |
| 3 | Recálculo completo desde historial (desde último `ADJUSTMENT`) | 0 filas |

La Parte 2 es la más útil para detectar **movimientos huérfanos** (comprometidos fuera de la transacción exterior). Este fue el patrón del bug `REM-2026-0001` del 18/04/2026.

> No modifica datos. Se puede ejecutar en producción sin riesgo.

---

## Uso paso a paso

### 1. Modo seguro (solo revisar)

Ejecuta el script tal cual. Termina con `ROLLBACK`, así que **no hace cambios reales**. Solo verás los conteos de filas antes y después de la simulación.

```bash
# Docker (desarrollo) - ventas
docker exec -i app-prod-mysql mysql -u root -p < tools/db/reset-sales.sql

# Docker (desarrollo) - catálogo de productos


```

### 2. Ejecutar de verdad

Edita el archivo y al final:
- Comenta la línea `ROLLBACK;`
- Descomenta la línea `-- COMMIT;`

```sql
-- ROLLBACK;  ← comentar esta línea
COMMIT;       ← descomentar esta línea
```

Luego vuelve a ejecutar.

### 3. Reiniciar AUTO_INCREMENT (opcional)

Si quieres que los nuevos registros empiecen con ID=1, descomenta el bloque de `ALTER TABLE ... AUTO_INCREMENT = 1` antes del COMMIT.

---

---

## Respaldo de base de datos de producción

MySQL en producción **no expone puerto al host** — solo es accesible dentro de `app-network`.
El respaldo se hace con `mysqldump` ejecutado dentro del contenedor vía `docker exec` sobre
el contexto SSH remoto (`cleeny`).

### 1. Verificar nombre del contenedor

```bash
docker --context cleeny ps --format "table {{.Names}}\t{{.Status}}"
```

El nombre sigue el patrón `<CONTAINER_PREFIX>-mysql` (valor en `.env.production`).

### 2. Generar el dump localmente

```bash
# La salida se redirige al host local — no ocupa espacio en el servidor
docker --context cleeny exec <CONTAINER_PREFIX>-mysql \
  mysqldump -u root -p<MYSQL_ROOT_PASSWORD> <MYSQL_DATABASE> \
  > backup_prod_$(date +%Y%m%d).sql
```

Sustituye `<CONTAINER_PREFIX>`, `<MYSQL_ROOT_PASSWORD>` y `<MYSQL_DATABASE>`
con los valores de tu `.env.production`.

### 3. Restaurar en entorno local (para pruebas)

```bash
# Opción A — contra el contenedor de desarrollo
docker exec -i <CONTAINER_PREFIX>-mysql \
  mysql -u root -p<MYSQL_ROOT_PASSWORD> <MYSQL_DATABASE> \
  < backup_prod_<fecha>.sql

# Opción B — contra MySQL local expuesto en 3306
mysql -h 127.0.0.1 -P 3306 -u root -p<password> <database> < backup_prod_<fecha>.sql
```

### 4. Respaldo automático en el servidor (cron)

```bash
# Conectarse al servidor y editar el crontab:  crontab -e
0 3 * * * docker exec <CONTAINER_PREFIX>-mysql \
  mysqldump -u root -p<MYSQL_ROOT_PASSWORD> <MYSQL_DATABASE> \
  | gzip > /backups/cleeny_$(date +\%Y\%m\%d).sql.gz
```

---

## Obtener credenciales Docker

Las credenciales están en el archivo de entorno del proyecto:

```bash
# Desarrollo
cat .env.development | grep -E "MYSQL_USER|MYSQL_PASSWORD|MYSQL_DATABASE|CONTAINER_PREFIX"

# Producción
cat .env.production | grep -E "MYSQL_ROOT_PASSWORD|MYSQL_DATABASE|CONTAINER_PREFIX"
```
