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

## Uso paso a paso

### 1. Modo seguro (solo revisar)

Ejecuta el script tal cual. Termina con `ROLLBACK`, así que **no hace cambios reales**. Solo verás los conteos de filas antes y después de la simulación.

```bash
# Docker (desarrollo)
docker exec -i cleeny-mysql mysql -u root -p < tools/db/reset-sales.sql
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

## Obtener credenciales Docker

Las credenciales están en el archivo de entorno del proyecto:

```bash
# Desarrollo
cat .env.development | grep -E "DB_USER|DB_PASSWORD|DB_NAME"
```
