### ADR-006: Impresión Térmica Directa vía Web Serial API

**Estado:** Aceptado
**Fecha:** 2026-04-01

**Contexto:**
La aplicación imprime tickets de venta directamente a impresoras térmicas Epson TM-T20IV-L desde el navegador, sin pasar por un diálogo de impresión del OS. La implementación original enviaba todos los bytes ESC/POS de golpe y calculaba un delay basado en baud rate serial (9600 bps) antes de cerrar el puerto. Esto causaba que los tickets se cortaran frecuentemente, especialmente en recibos con logo o código QR.

**Problema raíz:**

1. **`writer.releaseLock()` no hace flush** — los bytes quedaban en el buffer del browser sin garantía de entrega al driver del OS.
2. **Baud rate irrelevante** — la TM-T20IV-L es USB 2.0 (12 Mbps); el parámetro `baudRate: 9600` es ignorado por el Epson TM Virtual Port Driver. El cálculo `bytes / 960 * 1000` subestimaba el tiempo mecánico real.
3. **Payload enviado de golpe** — recibos con logo raster + QR pueden superar 10-20 KB, desbordando el buffer de recepción de 4 KB de la impresora.

**Decisión:**

1. **Envío en chunks**: Los datos se envían en bloques configurables (`PrintChunkSize`, default 2048 bytes). Entre cada chunk, `await writer.ready` provee back-pressure natural del stack USB/driver, evitando desbordar el buffer de 4 KB de la impresora.

2. **`writer.close()` en vez de `writer.releaseLock()`**: Según la spec de Web Serial, `writer.close()` hace flush del writable stream al buffer del driver del OS. `releaseLock()` solo suelta el lock sin flush.

3. **`port.close()` como flush final**: La spec de Web Serial establece que `port.close()` "flush the contents of all software and hardware transmit buffers for the port" antes de completar. Esto garantiza que todos los bytes llegan físicamente a la impresora.

4. **Safety buffer configurable**: Se mantiene un delay configurable (`PrintFlushDelayMs`, default 500 ms) entre `writer.close()` y `port.close()` como defensa en profundidad. En teoría `writer.close()` + `port.close()` son suficientes, pero el delay protege contra implementaciones de driver que no respeten la semántica de flush al 100%. El cálculo automático adicional (1 ms por cada 8 bytes, piso de 300 ms) se suma al buffer configurable.

5. **Ambos parámetros configurables desde UI**: En Admin → Settings → Ticket, el usuario puede ajustar tanto el safety buffer (ms) como el chunk size (bytes) sin necesidad de cambiar código.

**Especificaciones de la Epson TM-T20IV-L:**

| Spec | Valor |
|------|-------|
| Buffer de recepción | 4 KB (configurable a 128 bytes vía Memory Switch) |
| Velocidad de impresión | 250 mm/sec |
| Interfaz | USB 2.0 Full Speed (12 Mbps) |
| ASB (Automatic Status Back) | Soportado vía `GS a` |
| Tiempo mecánico del cutter | ~200 ms |
| BUSY release | Cuando quedan ~256 bytes libres en buffer |

6. **Health check con `DLE EOT`**: Antes de enviar datos, se envía el comando real-time `DLE EOT n=1` (`0x10 0x04 0x01`) y se lee la respuesta de 1 byte. Este comando bypasea el buffer de recepción y es procesado inmediatamente — detecta una impresora apagada o no responsiva en ~1.5s (timeout) sin tener que esperar a que falle el envío completo. Si no hay respuesta, se aborta y se cae al fallback PDF.

7. **Confirmación de impresión con `GS r`**: Se appends `GS r n=1` (`0x1D 0x72 0x01` — Transmit paper sensor status) al final del payload, después del comando de corte. `GS r` es un comando normal (no real-time) — se procesa FIFO. La respuesta de 1 byte solo llega cuando la impresora procesó **todo** lo anterior (incluyendo el corte). Esto reemplaza el delay heurístico por una confirmación real de impresión completa. Además, el byte de respuesta incluye el estado del papel:
   - Bits 2-3: `0x00` = papel OK, `0x0C` = papel casi vacío
   - Bits 5-6: `0x00` = papel presente, `0x60` = sin papel

8. **Feedback de diagnóstico al servidor**: Las funciones JS retornan un objeto `{success, bytesSent, paperStatus, error}` en vez de booleano. El `ThermalPrinterService` en C# lo mapea a `DirectPrintResult` y loguea con Serilog: bytes enviados, estado del papel, y errores específicos (`no-port`, `open-timeout`, `no-signal`, `dle-eot-timeout`, `send-error`). Si el papel está "near-end" o "empty" se loguea como warning.

**Flujo de datos:**

```
Browser JS                    OS / Driver                 Printer
─────────────────────────────────────────────────────────────────
DLE EOT (0x10 0x04 0x01) ──→  real-time cmd  ──→  bypasses buffer  ──→  immediate 1-byte response
  read response (1.5s timeout)                                            (printer status)
writer.write(chunk)  ──→  OS serial buffer  ──→  USB bulk transfer  ──→  4 KB receive buffer
  ... data + cut + GS r 1        (back-pressure)         (12 Mbps)        (process FIFO at print speed)
writer.close()       ──→  flush to driver
  read GS r response  ←──────────────────────────────────────────────  ←  paper status byte (completion!)
port.close()         ──→  flush HW TX buffers
  return {success, bytesSent, paperStatus, error} → C# logs to Serilog
```

**Archivos involucrados:**

| Archivo | Rol |
|---------|-----|
| `src/App.Web/wwwroot/js/webserial-print.js` | Bridge JS: DLE EOT, chunking, GS r confirmation, structured result |
| `src/App.Web/Services/ThermalPrinterService.cs` | Backend: JSInterop, `DirectPrintResult` mapping, Serilog logging |
| `src/App.Models/Settings/TicketConfiguration.cs` | Entity: `PrintFlushDelayMs`, `PrintChunkSize` |
| `src/App.Core/DTOs/Ticket/TicketConfigurationDto.cs` | DTO de lectura |
| `src/App.Core/DTOs/Ticket/UpdateTicketConfigurationDto.cs` | DTO de escritura |
| `src/App.Services/Tickets/TicketService.cs` | Mapeo entity ↔ DTO |
| `src/App.Web/Components/Admin/Settings/Ticket/TicketConfigurationTab.razor` | UI de configuración |

**Alternativas consideradas:**

- **Epson ePOS SDK (HTTP/WebSocket al puerto 8008)**: Provee confirmación real de impresión via `ASB_PRINT_SUCCESS`, pero requiere que la impresora tenga interfaz de red o ePOS Print service instalado. La TM-T20IV-L solo tiene USB + RS-232.
- **ASB bidireccional**: Abrir un reader en `port.readable` y esperar status idle del printer vía `GS a`. Más robusto pero agrega complejidad significativa al JS.
- **Sin delay, solo flush**: Depender únicamente de `writer.close()` + `port.close()`. Viable en teoría, pero el safety buffer configurable es defense-in-depth sin costo perceptible para el usuario.

**Consecuencias:**

- Los tickets ya no se cortan independientemente del tamaño del payload.
- El chunk size y safety buffer son ajustables por el usuario desde la UI sin deploy.
- El default de `PrintFlushDelayMs` sube de 200 a 500 ms (requiere migración `AddPrintChunkSize`).
- Futuro: al agregar datos de factura electrónica (CFDI) al ticket, el payload crecerá pero el mecanismo escala sin cambios — el chunking y flush manejan cualquier tamaño.

**Compatibilidad con otras impresoras:**

ESC/POS es un estándar creado por Epson pero adoptado por la mayoría de fabricantes de impresoras térmicas POS. La implementación actual tiene capas universales y capas Epson-specific:

| Capa | ¿Universal? | Detalle |
|------|-------------|---------|
| Comandos ESC/POS (text, cut, QR, raster) | Sí | Star, Bixolon, Citizen, Sewoo, etc. |
| `DLE EOT` (health check real-time) | Sí | Parte del estándar ESC/POS |
| `GS r` (paper status / print completion) | Sí | Parte del estándar ESC/POS |
| Web Serial API (Chrome/Edge) | Sí | Funciona con cualquier puerto serial/USB-serial |
| DSR/CTS signal check | Depende | El comportamiento (de-assert al desconectar USB) es del TM Virtual Port Driver; otros drivers pueden diferir |
| Epson TM Virtual Port Driver | No | Solo Epson; otras marcas usan drivers USB-CDC genéricos o propios |

Para soportar otra marca: si la impresora expone un puerto COM (real o virtual), el código ESC/POS funciona sin cambios. Solo cambiaría la forma de instalar el driver en el cliente y posiblemente el check de señales DSR/CTS. El code page PC850 (`ESC t 2`) y los comandos de QR/raster son compatibles con la mayoría de impresoras ESC/POS del mercado.

**Deuda técnica (baja prioridad):**

- **`GS v 0` obsoleto**: La función `_imageToEscPos()` en `webserial-print.js` usa `GS v 0` (Print raster bit image) para el logo de la empresa. Epson lo marca como obsoleto en la [referencia ESC/POS](https://download4.epson.biz/sec_pubs/pos/reference_en/escpos/gs_lv_0.html) y recomienda `GS ( L` / `GS 8 L` (Store + Print graphics data) como reemplazo. La TM-T20IV-L soporta ambos y `GS v 0` funciona correctamente — no hay urgencia. Migrar solo si: se cambia de modelo de impresora, el logo presenta defectos visuales, o se necesita el control adicional que `GS ( L` ofrece (almacenamiento separado del print, mejor manejo de memoria).

**Referencias:**

- [ESC/POS Command Reference (Epson)](https://download4.epson.biz/sec_pubs/pos/reference_en/escpos/index.html)
- [Web Serial API Spec (WICG)](https://wicg.github.io/serial/)
- [TM-T20IV Technical Reference Guide](https://download4.epson.biz/sec_pubs/bs/pdf/TM-T20IV_trg_en_revA.pdf)
