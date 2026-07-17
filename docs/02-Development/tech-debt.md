# Deuda Técnica

Lista breve de problemas conocidos que no son bugs activos pero representan un riesgo latente o una limitación de diseño que conviene atender. Una línea por ítem; el detalle completo (diagnóstico, logs, decisión) vive en el incident-log o el ADR referenciado.

---

+ Índice único `IX_sh_product_wholesale_prices_ProductId_WholesaleTierId` está declarado en el modelo EF con `HasFilter("IsDeleted = 0")`, pero Pomelo/MySQL ignora silenciosamente ese filtro — en producción el índice es único de forma global, no solo entre filas activas (confirmado con `SHOW CREATE TABLE` en producción). No es la causa de ningún bug conocido hoy, pero reconfigurar un nivel de mayoreo después de que su fila anterior fue soft-deleted puede chocar con esta unicidad. Detalle: [incident-log.md — 2026-07-17](incident-log.md).
