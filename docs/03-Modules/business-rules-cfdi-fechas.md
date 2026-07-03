# Reglas de Negocio: Fechas de CFDI (Facturación México)

Contexto arquitectónico: [ADR-009](../01-Architecture/adr/0009-fechas-pdf-cfdi-y-regeneracion.md).

## 1. Las tres fechas de una factura CFDI individual

Una factura CFDI (`MexicoInvoice`) puede tener hasta tres fechas relevantes, cada una con un significado distinto. No son intercambiables.

| Campo (entidad) | Significado | Se antedata | Fuente |
|---|---|---|---|
| `RequestedInvoiceDate` | Fecha que el usuario eligió como fecha del CFDI (ej. la fecha de ingreso del pedido), cuando decide **antedatar** la factura | Sí — es el mecanismo de antedatado | Elegida por el usuario en el diálogo de facturación (`dto.InvoiceDate`), dentro de la ventana máxima configurada (`TaxSettings.MxMaxBackdateHours`) |
| `StampDate` | Momento real en que el PAC certificó (timbró) el CFDI | No — siempre es "ahora" en el momento del timbrado | Respuesta del PAC (`SwSapienStampData`) |
| `cfdi:Comprobante/@Fecha` (nodo del XML) | Fecha fiscal oficial del comprobante ante el SAT | Igual a `RequestedInvoiceDate` si existe; si no, igual al momento de emisión | Calculada como `issueDate` antes de `BuildComprobante()` |

**Regla:** `RequestedInvoiceDate` (cuando existe) y el nodo `Fecha` del XML **siempre deben coincidir**. Es la misma variable (`issueDate`) usada en ambos flujos de timbrado (`CreateAndStampAsync`, `RetryStampAsync`).

## 2. Qué fecha muestra cada superficie

| Superficie | Campo mostrado | Regla |
|---|---|---|
| Lista de facturas (`InvoicesPage.razor`) | `RequestedInvoiceDate ?? StampDate ?? CreatedAt`, con `StampDate` como subtítulo "Timbrado: ..." cuando ambas existen | Prioriza la fecha fiscal (antedatada) como dato principal; el timbrado real queda como referencia secundaria |
| PDF — "FECHA DE EMISIÓN" | `RequestedInvoiceDate ?? StampDate` (vía `CfdiPdfDateResolver.Resolve`) | Debe coincidir con el nodo `Fecha` del XML — es el mismo dato, no una aproximación |
| PDF — "FECHA DE CERTIFICACIÓN" | `StampDate` siempre | Es evidencia de auditoría de cuándo el PAC certificó el documento; nunca se antedata, aunque la factura completa sí lo esté |
| XML (`cfdi:Comprobante/@Fecha`) | `issueDate` (= `RequestedInvoiceDate` convertido a hora local del emisor, o "ahora" si no hay antedatado) | Fuente de verdad fiscal — todo lo demás debe ser consistente con esto |

## 3. Todas las fechas se almacenan en UTC, se muestran en hora local del emisor

- **Almacenamiento:** `RequestedInvoiceDate`, `StampDate`, `CancellationDate` se guardan siempre en UTC (convención del proyecto — ver CLAUDE.md, "All DateTime handling uses UTC internally").
- **Zona horaria de conversión para mostrar:** la del **emisor**, resuelta en este orden:
  1. `TaxSettings.PostalCodeIanaTimeZoneId` (zona horaria del código postal fiscal del emisor, catálogo `cat_cfdi_postal_codes` — ver memoria de proyecto "CFDI Postal Code Timezone")
  2. Si no está configurado: zona horaria de la empresa (`ICompanySettingsService.GetCurrentTimeZoneAsync()`)
- **Nunca** se debe formatear una fecha UTC directamente (`.ToString(...)` sin conversión previa) en ninguna superficie visible al usuario — ni en la lista, ni en el PDF, ni en reportes. Ver [ADR-007 sección 6](../01-Architecture/adr/0007-factura-global-publica-en-general.md) para el mismo principio aplicado a Facturas Globales.

## 4. Regeneración de PDF sobre factura ya timbrada

- El **XML timbrado es inmutable** una vez recibido del PAC — nunca se regenera ni se vuelve a firmar.
- El **PDF (representación impresa)** sí puede regenerarse a partir de los mismos datos ya timbrados, para corregir errores de formato/datos en el documento visual (p. ej. el bug de fechas de este ADR).
- Regenerar el PDF cuando **ya existe uno** requiere que `MexicoPacSettings.AllowPdfRegenerationForStampedInvoices = true` (Administración > Configuración > Facturación > "Permitir regenerar el PDF de facturas ya timbradas"). Por defecto está **desactivado**.
- Generar el PDF por primera vez (p. ej. el timbrado tuvo éxito pero la generación de PDF falló) **no** requiere el flag — siempre está permitido.
- Cancelar una factura regenera automáticamente su PDF con la marca de agua "CANCELADA" (`RegenerateCancelledPdfAsync`) — este flujo no depende del flag, porque no sobrescribe un PDF "vigente", sino que refleja el nuevo estado (cancelado) del documento.

## 5. Ventana de antedatado

- `TaxSettings.MxMaxBackdateHours` limita cuántas horas hacia atrás se puede fijar `RequestedInvoiceDate` respecto al momento actual (hora local del emisor). Si no está configurado, no hay límite.
- No se puede antedatar hacia el futuro: `dto.InvoiceDate` no puede exceder "ahora + 5 minutos" (margen de tolerancia por desfases de reloj).
