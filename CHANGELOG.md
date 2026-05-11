# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Fixed

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
