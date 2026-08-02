# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

## [1.1.0] - 2026-08-01

### Added

- **Conteos Físicos de Inventario (Physical Inventory Counts)** — nuevo módulo para levantar
  conteos físicos por ubicación, comparar contra el inventario del sistema y generar ajustes
  de las diferencias encontradas. Incluye página de listado, detalle, diálogo de revisión,
  PDF de conteo y configuración de inventario (`InventorySettingsTab`) en Settings.
- **Control de acceso a ubicaciones (Location Access Control)** — los usuarios ahora pueden
  restringirse a una o varias ubicaciones específicas (`UserHasGlobalLocationAccess`); si no
  tienen acceso global, los servicios de inventario filtran automáticamente movimientos,
  historial y consultas a solo sus ubicaciones asignadas. Se agregó `InventoryLocationAccessTests`
  (18 tests) cubriendo la restricción.
- **Transferencia masiva de inventario (Bulk Transfer)** — nueva página `/shop/inventory` →
  pestaña de transferencias para mover múltiples productos entre ubicaciones en una sola
  operación, con PDF de comprobante (`BulkTransferDocument`) y `BatchNumber`/`BatchId` en
  `InventoryMovement` para agrupar los movimientos de un mismo lote.
- Al abrir una nueva sesión de caja, el diálogo (`OpenCashRegisterDialog`) ahora muestra el
  saldo final del último corte de caja cerrado, para que el cajero pueda verificarlo antes de
  iniciar turno.

### Changed

- **`ICurrentUserService` convertido a totalmente asíncrono** — se eliminaron todas las
  propiedades sync-over-async (`.Result`, `.GetAwaiter().GetResult()`) que podían causar
  deadlocks en Blazor Server bajo carga concurrente. Ver
  `docs/01-Architecture/adr/0010-acceso-async-usuario-actual.md` y el incidente documentado en
  `docs/02-Development/incident-log.md` (2026-07-25). Afecta a la mayoría de los servicios que
  dependían del usuario/ubicación actual.
- Refactor de `SaleService` y manejo de transacciones: se amplió la cobertura de
  `TransactionIntegrityTests` y se documentaron los hallazgos en `incident-log.md` y
  `tech-debt.md`.

### Fixed

- **Factura CFDI — columna "Importe" del PDF mostraba el total con IVA incluido** — en el PDF
  de cortesía de la factura, "Importe" tomaba `SaleDetail.Total` (con IVA) mientras "Precio U."
  mostraba el precio sin IVA, rompiendo la relación Cantidad × Precio = Importe a nivel de
  renglón (el XML CFDI timbrado ya calculaba el importe correctamente, sin IVA). Solución:
  `MexicoInvoiceService.cs` ahora usa `SaleDetail.Subtotal` (sin IVA) para "Importe" en los 4
  puntos donde se arma el PDF, consistente con el XML y con "Precio U.".
- **"Consolidar Remisiones" rechazaba un pago exacto por $0.01** — al consolidar una remisión
  en una venta, se le volvía a aplicar redondeo de caja a un total que ya había quedado
  congelado sin redondeo al crear la remisión, sumando $0.01 de más y rechazando el pago que el
  cliente ya había hecho por el monto exacto. Mismo patrón de fondo que un incidente previo con
  la conversión de Cotización→Venta (ver `docs/02-Development/incident-log.md`, 2026-08-01).
  Solución: `CreateSaleDto` gana una propiedad explícita `ApplyRounding` (en vez de inferirse de
  `QuotationId`/`SaleType`); `RemissionService.ConsolidateAsync` y las 2 páginas de conversión
  de cotización la fijan en `false`. Se agregó también una validación de cambio de tasa de IVA
  entre la creación de la remisión y su consolidación, espejo de la que ya existía para
  cotizaciones. Cubierto por `SaleServiceRoundingTests` (nuevo test
  `ConsolidateRemission_WithRoundingEnabled_ReproducesRemissionTotalWithoutRounding`).
- **Deadlock en Blazor Server por llamadas sync-over-async en `CurrentUserService`** — bajo
  ciertas condiciones de concurrencia la UI se congelaba al resolver el usuario/ubicación
  actual de forma síncrona sobre código asíncrono. Solución: conversión completa a async (ver
  arriba) más un fix previo puntual en `CurrentUserService.cs`.
- **Precio de producto edita "sin errores" pero no guarda precios de mayoreo** — Al editar un
  producto con precio de venta ≥ $1,000 y configurar un nivel de mayoreo, el guardado fallaba
  silenciosamente (toast de éxito del producto se mostraba igual, el warning real quedaba
  opacado). Causa: `ProductWholesalePrice.FixedPrice` era `decimal(9,6)` (máx. $999.999999)
  mientras `Product.Price` es `decimal(10,6)` — cualquier producto de $1,000+ hacía que MySQL
  rechazara el guardado completo con `Out of range value for column 'FixedPrice'`. Verificado
  en logs y BD de producción (ver `docs/02-Development/incident-log.md`, 2026-07-17).
  Solución: migración `WidenWholesaleFixedPricePrecision` amplía la columna a `decimal(10,6)`;
  `ValidateWholesaleFixedPrice` ahora rechaza en el formulario lo que la columna no puede
  guardar; el toast de "Producto actualizado con éxito" ya no se muestra si falla el guardado
  de precios de mayoreo.
- **Desactivar/limpiar un nivel de mayoreo no se guardaba** — Poner un nivel ya configurado de
  vuelta en 0/inactivo no persistía: el filtro "solo guardar datos con valores válidos" en
  `SaveWholesalePrices` also impedía enviar la solicitud de *borrado* al backend cuando todos
  los niveles quedaban en cero, así que la fila existente en la BD nunca se tocaba. Además, un
  nivel sin fila en la BD se mostraba con el checkbox "Activo" marcado por un valor por default
  incorrecto (`existing?.IsActive ?? true`), aparentando estar configurado sin estarlo.
  Solución: se agregó `HadExistingRow` al modelo de cada nivel para que limpiar una fila
  existente siga disparando la limpieza en el servidor; el default de `IsActive` para niveles
  sin fila pasó a `false`.
- **Descuento mayoreo — error $0.01 al convertir cotización a venta** — El total recalculado
  de la venta difería del total de la cotización cuando la línea tenía precio fijo de mayoreo,
  bloqueando la validación de pago. Causa: `DiscountAmount` almacenado a 2 decimales y el
  porcentaje derivado redondeado causaba deriva al recalcular. Solución: precisión subida a
  `decimal(18,6)` en detalles de cotización y remisión; la conversión ahora pasa el monto
  exacto de descuento en lugar de recalcular desde el porcentaje.

### Changed

- Lógica de resolución de descuento mayoreo centralizada en
  `PricingCalculationService.ResolveWholesaleDiscount` — elimina implementaciones duplicadas
  en Ventas, Cotizaciones y Remisiones.
- Regla de precio fijo de mayoreo almacena `FixedDiscountAmountPerUnit` (monto exacto) en
  lugar de convertirlo a porcentaje, evitando pérdida de precisión.

### Added

- `WholesaleDiscountTests` — 15 tests unitarios e integración que cubren resolución de tiers,
  descuento por porcentaje, precio fijo, y el caso de regresión del bug $0.01.
