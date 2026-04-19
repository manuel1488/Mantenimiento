-- ============================================================
-- SCRIPT: inventory-reconciliation.sql
-- Conciliación de saldos de inventario vs historial de movimientos.
--
-- Propósito: verificar que los valores en sh_inventory coincidan
-- con el historial completo de sh_inventory_movements. Útil para
-- detectar movimientos huérfanos, inconsistencias o corrupción
-- de datos producida por bugs transaccionales.
--
-- Este script es de solo lectura (solo SELECT). No modifica datos.
--
-- Uso:
--   docker exec -i <contenedor-mysql> mysql -u root -p<pass> <db> \
--     < tools/db/inventory-reconciliation.sql
-- ============================================================


-- ============================================================
-- PARTE 1: Inventario actual vs último movimiento registrado
--
-- Compara sh_inventory.Quantity con el NewBalance del último
-- movimiento (por Id) de cada producto+ubicación.
--
-- Resultado esperado: 0 filas
-- Si hay filas: el saldo actual difiere del cierre del último
-- movimiento — indica corrupción directa del saldo.
--
-- NOTA: Productos con Quantity=0 y sin historial se excluyen
-- intencionalmente (son consistentes: 0 = 0).
-- ============================================================
SELECT
    'PARTE 1 - Saldo actual vs último movimiento' AS seccion;

SELECT
    p.Name                                              AS producto,
    l.Name                                              AS ubicacion,
    i.Quantity                                          AS saldo_actual,
    last_m.NewBalance                                   AS saldo_ultimo_movimiento,
    (i.Quantity - last_m.NewBalance)                    AS diferencia,
    last_m.MovementType                                 AS tipo_ultimo_mov,
    last_m.MovementDate                                 AS fecha_ultimo_mov,
    last_m.Reference                                    AS referencia_ultimo_mov
FROM sh_inventory i
JOIN sh_products  p ON p.Id = i.ProductId
JOIN sh_locations l ON l.Id = i.LocationId
LEFT JOIN (
    SELECT m1.ProductId, m1.LocationId,
           m1.NewBalance, m1.MovementType, m1.MovementDate, m1.Reference
    FROM sh_inventory_movements m1
    WHERE m1.Id = (
        SELECT MAX(m2.Id)
        FROM sh_inventory_movements m2
        WHERE m2.ProductId  = m1.ProductId
          AND m2.LocationId = m1.LocationId
    )
) last_m ON last_m.ProductId  = i.ProductId
        AND last_m.LocationId = i.LocationId
WHERE ABS(i.Quantity - COALESCE(last_m.NewBalance, 0)) > 0.000001
   OR (last_m.ProductId IS NULL AND i.Quantity <> 0)
ORDER BY ABS(i.Quantity - COALESCE(last_m.NewBalance, 0)) DESC;


-- ============================================================
-- PARTE 2: Integridad de la cadena de movimientos
--
-- Para cada movimiento, PreviousBalance debe ser igual al
-- NewBalance del movimiento inmediatamente anterior (por Id)
-- del mismo producto+ubicación.
--
-- Una ruptura indica un movimiento huérfano (comprometido fuera
-- de la transacción exterior) o una edición manual incorrecta.
-- Este fue el patrón del bug REM-2026-0001.
--
-- Resultado esperado: 0 filas
-- ============================================================
SELECT
    'PARTE 2 - Integridad de cadena de movimientos' AS seccion;

SELECT
    m.Id                                                AS movimiento_id,
    p.Name                                              AS producto,
    l.Name                                              AS ubicacion,
    m.MovementType                                      AS tipo,
    m.MovementDate                                      AS fecha,
    m.Reference                                         AS referencia,
    prev_m.NewBalance                                   AS saldo_anterior_esperado,
    m.PreviousBalance                                   AS saldo_anterior_registrado,
    (m.PreviousBalance - prev_m.NewBalance)             AS diferencia
FROM sh_inventory_movements m
JOIN sh_products  p ON p.Id = m.ProductId
JOIN sh_locations l ON l.Id = m.LocationId
LEFT JOIN sh_inventory_movements prev_m
    ON prev_m.ProductId  = m.ProductId
   AND prev_m.LocationId = m.LocationId
   AND prev_m.Id = (
       SELECT MAX(m3.Id)
       FROM sh_inventory_movements m3
       WHERE m3.ProductId  = m.ProductId
         AND m3.LocationId = m.LocationId
         AND m3.Id < m.Id
   )
WHERE prev_m.Id IS NOT NULL
  AND ABS(m.PreviousBalance - prev_m.NewBalance) > 0.000001
ORDER BY p.Name, l.Name, m.MovementDate;


-- ============================================================
-- PARTE 3: Recálculo completo desde historial
--
-- Recalcula el saldo esperado usando todos los movimientos:
--   - Toma el último ADJUSTMENT como punto de reset (si existe)
--   - Aplica todos los movimientos posteriores (entradas/salidas)
--   - Compara contra el saldo actual en sh_inventory
--
-- Tipos de entrada (suman): STOCK_IN, PURCHASE, RETURN, INITIAL_LOAD
-- Tipos de salida (restan): STOCK_OUT, SALE, RETURN_SUPPLIER, TRANSFER
-- ADJUSTMENT: fija saldo absoluto, se usa como base de cálculo
--
-- Resultado esperado: 0 filas
-- ============================================================
SELECT
    'PARTE 3 - Recálculo desde historial completo' AS seccion;

WITH base AS (
    -- Último ADJUSTMENT por producto+ubicación (punto de reset)
    -- Si no existe, la base es 0
    SELECT
        i.ProductId,
        i.LocationId,
        COALESCE(adj.NewBalance, 0) AS base_quantity,
        COALESCE(adj.Id, 0)         AS base_movement_id
    FROM sh_inventory i
    LEFT JOIN sh_inventory_movements adj
        ON adj.ProductId   = i.ProductId
       AND adj.LocationId  = i.LocationId
       AND adj.MovementType = 'ADJUSTMENT'
       AND adj.Id = (
           SELECT MAX(a2.Id)
           FROM sh_inventory_movements a2
           WHERE a2.ProductId   = i.ProductId
             AND a2.LocationId  = i.LocationId
             AND a2.MovementType = 'ADJUSTMENT'
       )
),
deltas AS (
    -- Suma neta de movimientos posteriores al último ADJUSTMENT
    SELECT
        m.ProductId,
        m.LocationId,
        SUM(CASE
            WHEN m.MovementType IN ('STOCK_IN', 'PURCHASE', 'RETURN', 'INITIAL_LOAD')
                THEN  m.Quantity
            WHEN m.MovementType IN ('STOCK_OUT', 'SALE', 'RETURN_SUPPLIER', 'TRANSFER')
                THEN -m.Quantity
            ELSE 0  -- ADJUSTMENT ya capturado en base; otros tipos desconocidos = neutro
        END) AS delta
    FROM sh_inventory_movements m
    JOIN base b
        ON b.ProductId  = m.ProductId
       AND b.LocationId = m.LocationId
    WHERE m.Id > b.base_movement_id
      AND m.MovementType <> 'ADJUSTMENT'
    GROUP BY m.ProductId, m.LocationId
)
SELECT
    p.Name                                              AS producto,
    l.Name                                              AS ubicacion,
    i.Quantity                                          AS saldo_actual,
    ROUND(b.base_quantity + COALESCE(d.delta, 0), 6)   AS saldo_calculado,
    ROUND(i.Quantity - (b.base_quantity + COALESCE(d.delta, 0)), 6) AS diferencia
FROM sh_inventory i
JOIN sh_products  p ON p.Id = i.ProductId
JOIN sh_locations l ON l.Id = i.LocationId
JOIN base b
    ON b.ProductId  = i.ProductId
   AND b.LocationId = i.LocationId
LEFT JOIN deltas d
    ON d.ProductId  = i.ProductId
   AND d.LocationId = i.LocationId
WHERE ABS(i.Quantity - (b.base_quantity + COALESCE(d.delta, 0))) > 0.000001
ORDER BY ABS(i.Quantity - (b.base_quantity + COALESCE(d.delta, 0))) DESC;
