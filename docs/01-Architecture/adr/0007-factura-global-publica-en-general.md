### ADR-007: Módulo de Factura Global — Ventas al Público en General (CFDI 4.0)

**Estado:** Aceptado  
**Fecha:** 2026-04-02

---

## Contexto

El SAT (Servicio de Administración Tributaria) de México obliga a los contribuyentes a amparar bajo CFDI todas las ventas realizadas. Las ventas al público en general (sin RFC del cliente) que no reciben CFDI individual deben agruparse en una **Factura Global** al cierre del período fiscal correspondiente.

Obligación legal: artículo 29 y 29-A del CFF y regla **2.7.1.24** de la Resolución Miscelánea Fiscal (RMF) vigente.

El complemento `InformacionGlobal` del CFDI 4.0 formaliza este proceso y requiere:
- RFC receptor fijo: `XAXX010101000`
- Nombre receptor: `PUBLICO EN GENERAL`
- Régimen fiscal receptor: `616`
- Uso CFDI: `S01`
- Periodicidad declarada: diaria, semanal, quincenal o mensual
- Cobertura de **todas** las ventas elegibles del período

---

## Decisión

Implementar el módulo de Factura Global con las siguientes restricciones y diseño:

### 1. Períodos fijos por tipo SAT (no rangos libres)

**Se rechazó** permitir al usuario seleccionar un rango de fechas libre. El motivo es que el nodo `InformacionGlobal` del CFDI exige declarar una periodicidad específica con fechas determinísticas:

| Periodicidad | Código SAT | Período |
|---|---|---|
| Diaria | `01` | Un solo día |
| Semanal | `02` | Lunes a Domingo |
| Quincenal | `03` | 1-15 ó 16-fin de mes |
| Mensual | `04` | Primero al último día del mes |

El diálogo `GenerateGlobalInvoiceDialog` usa navegación `< Período >` con flechas, calculando las fechas automáticamente según la periodicidad activa. El botón "Siguiente" se deshabilita cuando el período aún no ha terminado.

### 2. Selección individual de ventas no soportada

**Se rechazó** permitir al usuario seleccionar manualmente qué ventas incluir. La razón fiscal es que la factura global debe cubrir **todas** las ventas al público del período (regla 2.7.1.24 RMF). Omitir ventas elegibles deja operaciones sin respaldo fiscal.

La excepción válida es que una venta tenga su propio CFDI individual — en ese caso el sistema la excluye automáticamente. El campo `AlreadyInvoicedCount` en el preview advierte cuántas ventas quedarán fuera por este motivo.

### 3. Una factura por período por forma de pago

El SAT permite emitir múltiples facturas globales para el mismo período siempre que correspondan a **formas de pago distintas** (ej. efectivo vs tarjeta). El campo `PaymentForm` en el diálogo soporta este caso. El servicio no bloquea dobles emisiones del mismo período/forma de pago — esa responsabilidad recae en el operador.

### 4. Ventas elegibles

Una venta se incluye en la factura global si cumple **todas** las condiciones:
- `SaleType == Public`
- `SaleStatus == Created` (no cancelada)
- No tiene `MexicoInvoice` activo (sin CFDI individual)
- No pertenece a ninguna `GlobalInvoice` activa

### 5. Concepto único agregado

El CFDI se genera con un solo concepto (conforme a la Guía de llenado CFDI global v4.0, sección I):
- **ClaveProdServ**: `01010101` (comodín para facturas globales)
- **ClaveUnidad**: `ACT`
- **Descripción**: `Venta al público en general`
- **Importe**: suma de todos los subtotales elegibles
- **IVA**: suma de todos los impuestos trasladados

No se desglosan productos individuales en el XML — esto es conforme al estándar SAT para facturas de público en general.

### 6. Formato de fechas en PDF

Todas las fechas UTC almacenadas en base de datos se convierten a la zona horaria de la empresa **en la capa de servicio** antes de pasarlas al DTO del PDF como strings pre-formateados. El `cshtml` view consume únicamente strings, nunca `DateTime`. Este patrón evita conversiones `ToLocalTime()` en la vista y garantiza consistencia con la configuración regional del negocio.

---

## Estructura de archivos

```plaintext
App.Core/
├── Constants/ApplicationClaims.cs          — Admin.GlobalInvoices.*, Billing.BillingAccess
├── DTOs/Billing/GlobalInvoiceDto.cs        — ListDto, Dto (+ Sales[]), PdfDto, CreateDto, PreviewDto, GlobalInvoiceSaleDto
├── Enums/Billing/
│   ├── GlobalInvoicePeriodicity.cs         — Daily, Weekly, Biweekly, Monthly
│   └── GlobalInvoiceStatus.cs             — Draft, Stamped, Cancelled, StampError
├── Interfaces/Billing/IGlobalInvoiceService.cs  — + GetActiveSaleToInvoiceMapAsync()

App.Models/Billing/
├── GlobalInvoice.cs                        — Entidad principal
└── GlobalInvoiceSale.cs                    — Tabla puente GlobalInvoice ↔ Sale

App.Models.Data/Migrations/
└── AddGlobalInvoices.cs                    — Tablas mx_global_invoices, mx_global_invoice_sales

App.Services/
├── Billing/GlobalInvoiceService.cs         — Preview, CreateAndStamp, GetAll, GetById (con Sales), GetXml, GetPdf, Cancel, GetActiveSaleToInvoiceMapAsync
└── Resources/Billing/
    ├── GlobalInvoiceService.en.resx
    └── GlobalInvoiceService.es.resx

App.Web/
├── Components/Admin/GlobalInvoices/
│   ├── GlobalInvoicesPage.razor            — Listado con icono Visibility → detalle
│   ├── GlobalInvoiceDetailPage.razor       — Vista detalle: info, emisor, ventas incluidas, totales
│   ├── GenerateGlobalInvoiceDialog.razor   — Navegador de períodos + preview + generar
│   └── CancelGlobalInvoiceDialog.razor     — Motivos SAT 01-04
├── Components/Shop/Sales/
│   └── SalesHistoryPage.razor              — Indicador de facturación global (ReceiptLong verde)
├── Views/GlobalInvoices/
│   └── GlobalInvoiceDocument.cshtml        — Plantilla PDF (Rotativa)
└── Resources/Components/Admin/GlobalInvoices/
    ├── GlobalInvoicesPage.{en,es}.resx
    ├── GlobalInvoiceDetailPage.{en,es}.resx
    ├── GenerateGlobalInvoiceDialog.{en,es}.resx
    └── CancelGlobalInvoiceDialog.{en,es}.resx

tests/App.Services.Tests/Billing/
└── GlobalInvoiceDetailTests.cs             — Integration tests: GetActiveSaleToInvoiceMapAsync + GetByIdAsync (Sales)
```

---

## Vista detalle y trazabilidad en historial de ventas

### Vista detalle (`/admin/global-invoices/{id}`)

`GlobalInvoiceDetailPage` muestra:
- Datos del CFDI: folio, UUID, fecha de timbrado, período, periodicidad, forma de pago
- Datos del emisor (snapshot al momento del timbrado)
- Sección de cancelación cuando aplica (motivo SAT, fecha, observaciones)
- **Tabla de ventas incluidas** con enlace a cada venta individual (`/shop/sales/{id}`)
- Panel de totales: subtotal / descuento / IVA / total

`GetByIdAsync` carga las ventas via `Include(GlobalInvoiceSales).ThenInclude(Sale)` y las expone en `GlobalInvoiceDto.Sales` ordenadas por fecha ascendente.

### Indicador en historial de ventas (`/shop/sales-history`)

Una venta puede tener tres estados de facturación, representados como iconos en la columna de acciones:

| Icono | Color | Condición | Acción |
|-------|-------|-----------|--------|
| `Receipt` relleno | Verde | CFDI individual activo | Navega a `/shop/invoices?saleId=...` |
| `ReceiptLong` relleno | Verde | Incluida en factura global timbrada | Navega a `/admin/global-invoices/{id}` |
| `Receipt` outlined | Gris | Sin factura | Abre diálogo de crear CFDI individual |

El mapa `saleId → globalInvoiceId` se construye en `GetActiveSaleToInvoiceMapAsync()`, que consulta `mx_global_invoice_sales` filtrando únicamente facturas con estado `Stamped`. Las ventas en facturas `Cancelled`, `Draft` o `StampError` quedan disponibles para ser incluidas en una nueva factura.

## Consecuencias

**Positivas:**
- Cumplimiento SAT: períodos fijos eliminan el riesgo de emitir CFDIs con `InformacionGlobal` inválido
- Simplicidad de UX: el operador no necesita recordar ni calcular fechas
- Trazabilidad: cada venta queda ligada a exactamente una factura global (o ninguna), visible desde el historial y desde el detalle de la factura

**Negativas:**
- No soporta retroactivos parciales: si se olvidó emitir una quincena pasada, hay que generarla como un período completo
- No permite agrupar por sucursal ni por vendedor: una factura global por período/forma de pago cubre todo el negocio

---

## Referencias

- [Regla 2.7.1.24 RMF — CFDI de operaciones con el público en general](https://wwwmat.sat.gob.mx/articulo/90959/regla-2.7.1.24) — regla vigente que habilita la factura global y define su periodicidad
- [Guía de llenado CFDI global v4.0](../../03-Modules/Guia_llenado_CFDI_global.md) — Reglas de llenado del nodo `InformacionGlobal`, concepto único agregado, RFC genérico XAXX010101000
- [Anexo 20 — Guía de llenado CFDI](../../03-Modules/Anexo_20_Guia_de_llenado_CFDI.md) — Especificación técnica completa del estándar CFDI 4.0
- ADR-003: Patrón de interfaz (Result pattern)
- ADR-005: Sistema de autenticación y autorización (Claims)
