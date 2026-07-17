# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Fixed

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
