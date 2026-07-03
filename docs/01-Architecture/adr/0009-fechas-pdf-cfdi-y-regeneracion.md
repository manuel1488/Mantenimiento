### ADR-009: Fechas del PDF de CFDI Individual y Regeneración de PDF Timbrado

**Estado:** Aceptado
**Fecha:** 2026-07-02

---

## Contexto

Se detectó que el PDF (representación impresa) de una factura CFDI individual (`MexicoInvoiceService`) podía mostrar fechas incorrectas cuando la factura era **antedatada** (emitida con la fecha de un evento anterior, p. ej. la fecha de ingreso de un pedido, en vez de la fecha real de timbrado).

Caso observado — factura A183:

| Fuente | Fecha mostrada |
|---|---|
| XML (`cfdi:Comprobante/@Fecha`) | `2026-06-30T13:30:00` (correcta — fecha de ingreso) |
| Lista de facturas (`InvoicesPage.razor`) | `30/06/2026 13:30` / `Timbrado: 01/07/2026 15:57` (correcta) |
| PDF — "FECHA DE EMISIÓN" | `01/07/2026 21:57` (incorrecta) |
| PDF — "FECHA DE CERTIFICACIÓN" | `01/07/2026 21:57:03` (incorrecta) |

Causa raíz — dos bugs independientes en `BuildInvoiceTemplateData` (`MexicoInvoiceService.cs`):

1. **Campo equivocado.** `issue_date` (FECHA DE EMISIÓN) usaba siempre `invoice.StampDate` (fecha real de timbrado), ignorando `invoice.RequestedInvoiceDate` — el mismo valor que `BuildComprobante` usa para el nodo `Fecha` del XML cuando la factura fue antedatada. Por eso el PDF y el XML podían divergir.
2. **Sin conversión de zona horaria.** Tanto `issue_date` como `stamp_date` se formateaban directamente desde UTC (`invoice.StampDate?.ToString(...)`) sin convertir a la zona horaria del emisor. La lista de facturas sí convierte (`DateTimeService.FormatToTimezone`), de ahí la discrepancia de 6 horas observada (offset de México).

Nótese que **ADR-007 (Factura Global), sección 6**, ya establece el patrón correcto — "todas las fechas UTC se convierten a la zona horaria de la empresa en la capa de servicio antes de pasarlas al DTO del PDF como strings pre-formateados" — pero ese patrón no se había aplicado consistentemente en `MexicoInvoiceService` (facturas individuales).

Adicionalmente, no existía forma de corregir el PDF de una factura que **ya tenía uno pero con datos incorrectos**: `RegeneratePdfAsync` y el botón correspondiente en `InvoicesPage.razor` solo estaban disponibles cuando la factura *no* tenía PDF todavía.

---

## Decisión

### 1. Resolución de fechas del PDF — `CfdiPdfDateResolver`

Se extrajo la lógica de resolución a una función pura y testeable (`App.Services/Billing/CfdiPdfDateResolver.cs`):

```csharp
(DateTime? IssueDateLocal, DateTime? StampDateLocal) Resolve(
    DateTime? requestedInvoiceDateUtc,
    DateTime? stampDateUtc,
    TimeZoneInfo issuerTimeZone)
```

Regla de negocio (ver también [business-rules-cfdi-fechas.md](../../03-Modules/business-rules-cfdi-fechas.md)):

- **FECHA DE EMISIÓN** (`issue_date`) = `RequestedInvoiceDate` cuando la factura fue antedatada; si no, cae a `StampDate`. Este valor **debe coincidir siempre con el nodo `Fecha` del XML** — es la misma fuente de verdad (`issueDate` calculado en `CreateAndStampAsync`/`RetryStampAsync` antes de `BuildComprobante`).
- **FECHA DE CERTIFICACIÓN** (`stamp_date`) = `StampDate` siempre — es el timestamp real que devuelve el PAC, independientemente de si la factura fue antedatada.
- Ambas fechas se convierten de UTC a la **zona horaria del emisor** (código postal fiscal vía `TaxSettings.PostalCodeIanaTimeZoneId`, con fallback a la zona horaria de la empresa) antes de formatearse — nunca se muestra un valor UTC crudo al usuario.

`ResolveIssuerTimeZoneAsync()` centraliza la resolución de zona horaria (antes duplicada de forma ad-hoc en 2 de los 4 flujos que generan PDF) y se reutiliza en los 4 puntos donde se construye el PDF: `CreateAndStampAsync`, `RetryStampAsync`, `RegeneratePdfAsync`, `RegenerateCancelledPdfAsync` (esta última también corrige `cancellation_date` con la misma conversión).

### 2. Regeneración de PDF sobre una factura que ya tiene uno — flag de configuración

`RegeneratePdfAsync` ya existía para el caso "timbrado exitoso pero la generación del PDF falló" (sin PDF previo). Se extendió para permitir **sobrescribir** un PDF ya existente, pero gateado por un ajuste explícito:

`MexicoPacSettings.AllowPdfRegenerationForStampedInvoices` (`bool`, default `false`).

**Por qué gatearlo y no habilitarlo siempre:**
- El PDF es la representación impresa de un documento fiscal ya emitido. Permitir su regeneración sin control facilita alterar accidentalmente un PDF ya entregado al cliente o archivado.
- El **XML timbrado nunca se toca** — solo el PDF (representación) se reconstruye a partir de los mismos datos ya firmados/timbrados. No hay riesgo fiscal de invalidar el CFDI, pero sí de generar confusión si un PDF distribuido cambia de contenido sin que el usuario lo autorice explícitamente a nivel de configuración.
- Por eso el default es `false`: cada negocio decide si delega esa capacidad a sus operadores.

El servicio valida el flag únicamente cuando **ya existe un PDF** para esa factura; la primera generación (o retry tras un fallo puntual sin PDF) sigue funcionando sin importar el valor del flag.

UI: `BillingSettingsTab.razor` (sección "Auto-Invoice Settings") expone el switch. `InvoicesPage.razor` muestra el botón "Regenerar PDF" junto al de "Descargar PDF" solo cuando el flag está activo.

---

## Alternativas consideradas

| Alternativa | Rechazada porque |
|---|---|
| Permitir regenerar el PDF siempre, sin flag | Ver el "por qué gatearlo" arriba — riesgo de confusión sobre documentos fiscales ya distribuidos |
| Recalcular `issue_date` a partir del XML almacenado en cada regeneración (parseando `cfdi:Comprobante/@Fecha`) | Añade una dependencia de parseo XML innecesaria cuando el mismo dato ya vive en `invoice.RequestedInvoiceDate`/`StampDate`; más frágil ante cambios de formato del XML |
| Mostrar `stamp_date` también antedatado cuando `RequestedInvoiceDate` está presente | Rompería la semántica: `stamp_date` es evidencia de cuándo el PAC realmente certificó el documento, útil para auditoría/soporte |

---

## Consecuencias

**Positivas:**
- El PDF y el XML de una factura antedatada muestran la misma "FECHA DE EMISIÓN", eliminando confusión ante el SAT o el cliente.
- Todas las fechas del PDF respetan la zona horaria del emisor, consistente con la lista de facturas y con ADR-007.
- Existe un mecanismo administrable para corregir PDFs con datos incorrectos ya generados, sin necesidad de re-timbrar.
- `CfdiPdfDateResolver` es una función pura, fácil de testear sin mocks de infraestructura.

**Negativas / riesgos:**
- Regenerar el PDF sobrescribe el archivo anterior — no se conserva versión histórica del PDF reemplazado (si se requiere auditoría de "qué PDF se envió cuándo", habría que añadir versionado a futuro).
- El flag es global (una sola configuración PAC por instalación), no por usuario ni por rol — cualquier operador con acceso a "Ver/Editar factura" puede regenerar si el flag está activo.

---

## Archivos relevantes

```plaintext
App.Services/Billing/
├── CfdiPdfDateResolver.cs                  — función pura de resolución de fechas locales
└── MexicoInvoiceService.cs
    ├── ResolveIssuerTimeZoneAsync()        — helper compartido de zona horaria
    ├── BuildInvoiceTemplateData(...)       — ahora recibe TimeZoneInfo issuerTimeZone
    └── RegeneratePdfAsync(...)             — gateado por AllowPdfRegenerationForStampedInvoices

App.Models/Billing/MexicoPacSettings.cs     — + AllowPdfRegenerationForStampedInvoices
App.Models.Data/Migrations/
└── AddPdfRegenerationSetting.cs
App.Core/DTOs/Billing/
├── MexicoPacSettingsDto.cs                 — + AllowPdfRegenerationForStampedInvoices
└── UpdateMexicoBillingPreferencesDto.cs    — + AllowPdfRegenerationForStampedInvoices

App.Web/Components/
├── Admin/Settings/Billing/BillingSettingsTab.razor  — switch "Allow regenerating PDF..."
└── Shop/Invoices/InvoicesPage.razor                 — botón "Regenerar PDF" condicionado al flag

tests/App.Services.Tests/Billing/
├── CfdiPdfDateResolverTests.cs                     — unit tests puros
└── MexicoInvoiceServicePdfRegenerationTests.cs     — integration tests (EF InMemory)
```

---

## Referencias

- ADR-007: Módulo de Factura Global — sección 6, "Formato de fechas en PDF" (patrón de conversión UTC→zona horaria en capa de servicio ya establecido para facturas globales)
- ADR-003: Patrón de interfaz (Result pattern)
- [Reglas de negocio: fechas de CFDI](../../03-Modules/business-rules-cfdi-fechas.md)
