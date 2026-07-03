# Troubleshooting Index

Quick reference for known issues and their diagnostic steps.

## Inventory

| Síntoma | Causa | Documento |
|---------|-------|-----------|
| Stock Actual muestra "–" en Nuevo Ajuste para un producto que sí tiene stock | Producto con `RequiresInventory = false` pero con registros en `sh_inventory` | [troubleshooting-inventory-adjustments.md](troubleshooting-inventory-adjustments.md) |

## Despliegue / Producción

| Síntoma | Causa | Documento |
|---------|-------|-----------|
| Deploy en crash-loop: `Duplicate check constraint name` / `Duplicate trigger` al migrar | Migración con DDL crudo (CHECK constraint / TRIGGER) interrumpida a medias en un deploy previo — MySQL hace commit implícito por sentencia, así que el objeto ya existe pero `__EFMigrationsHistory` no tiene la fila | [incident-log.md](incident-log.md) — bitácora de incidentes de producción |
