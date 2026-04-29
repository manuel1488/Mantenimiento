# Roadmap Estratégico: Evolución a SaaS POS Multi-Tenant

**Estado:** Aprobado  
**Fecha:** 2026-04-28  
**Autor:** Manuel Alfaro

---

## Visión

Convertir Cleeny en una plataforma SaaS multi-tenant de bajo costo, orientada a micro y pequeñas empresas mexicanas (1–10 empleados), con soporte de hardware POS completo (impresora térmica, caja registradora, báscula), operación en tablet, y capacidad offline progresiva.

---

## Arquitectura Objetivo

```
┌──────────────────────────────────────┐
│          Back-office Web             │
│      Blazor Server — actual          │
│  Inventario, reportes, facturación,  │
│  configuración, admin multi-tenant   │
│  Requiere conexión estable ✓         │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│         Terminal POS                 │
│     PWA / Blazor WASM (futuro)       │
│  Solo: ventas, cobro, ticket         │
│  Offline-capable                     │
│  Tablet-optimized                    │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│         Agente Local Hardware        │
│       .NET Windows Service/Tray      │
│  Impresora térmica (USB/COM)         │
│  Caja registradora (RJ11)            │
│  Báscula (COM/USB/Red) — futuro      │
└──────────────────────────────────────┘
```

Ambas capas de UI consumen el mismo backend API. Comparten `App.Core` y DTOs. El agente local abstrae todo el acceso a hardware.

---

## Fases

### Fase 1 — Agente Local de Hardware (Corto plazo)

**Objetivo:** Resolver los conflictos de impresión y sentar las bases del acceso a hardware sin depender del navegador.

**Contexto del problema actual:**
- La comunicación browser → ePOS SDK → Virtual COM Port falla cuando múltiples tabs compiten por el puerto
- COM es un recurso exclusivo: solo un proceso puede tenerlo abierto
- La caja registradora se abre vía el mismo canal que la impresora (RJ11 → impresora → ESC/POS)

**Diseño del agente:**
```
Blazor Web App
      │
      │ HTTP (localhost:9100)
      ▼
 Local Agent (.NET, tray/service)
  ├── Impresora térmica (USB / Virtual COM)
  ├── Caja registradora (vía impresora RJ11, comando ESC p)
  └── Báscula (COM/USB/Red) — fase 5
```

**Entregables:**
- Ejecutable .NET de distribución única (single-file)
- Endpoint HTTP local con autenticación por token simple
- Cola FIFO para serializar trabajos de impresión
- Configuración de dispositivo persistida localmente

**Impacto en el proyecto actual:**
- `ThermalPrinterService` en `App.Web/Services/` delega a `localhost:9100` en lugar del ePOS SDK del browser
- El JS de impresión queda como fallback (PDF en nueva pestaña)

---

### Fase 2 — Multi-Tenant Backend (Pre-requisito SaaS)

**Objetivo:** Preparar la arquitectura para múltiples clientes en producción compartiendo infraestructura.

**Decisión de estrategia de datos:** Shared DB con `TenantId`

Justificación:
- Más barato de operar (una sola instancia MySQL)
- Apropiado para low-cost SaaS con muchos tenants pequeños
- Complejidad de código manejable con middleware de tenant
- Migración futura a DB-per-tenant es posible si el mercado lo exige

**Cambios requeridos:**
- `TenantId` en todas las tablas de negocio (`sh_*`, `mx_*`)
- `ITenantService` para resolver tenant desde subdomain o claim
- Middleware en pipeline HTTP que inyecta el tenant activo
- `ApplicationDbContext` filtra automáticamente por `TenantId`
- Self-service signup con onboarding automatizado
- Configuración por tenant en BD (actualmente en `appsettings.json`)
- Plan/suscripción básico (puede iniciar con stripe o manual)

---

### Fase 3 — Terminal POS PWA Tablet-Optimized

**Objetivo:** Un segundo cliente de UI, optimizado para tablets (Android/iPad), con UX enfocada solo en el flujo de venta.

**Por qué PWA y no app nativa:**
- Sin costo de distribución (no requiere App Store/Play Store)
- Actualizaciones automáticas desde el servidor
- Instalable en Android desde el browser (icono en home screen)
- Reusa DTOs y lógica de `App.Core`

**Tecnología:**
- Blazor WASM (máxima reutilización de componentes existentes) o frontend ligero según evaluación
- Service Worker para caching de assets y catálogo de productos
- IndexedDB para persistir ventas pendientes offline
- Conecta al mismo API backend del Blazor Server actual

**Funcionalidades del terminal:**
- Búsqueda y selección de productos
- Cobro (efectivo, tarjeta, mixto)
- Generación de ticket (impresión vía agente en red o impresora WiFi/BT)
- Apertura de caja registradora
- Sin acceso a inventario, reportes, ni configuración (eso es back-office)

**Hardware en tablet:**
- Impresoras WiFi/Bluetooth (Epson TM-T20 WiFi, Star Micronics) — sin agente necesario
- Lectores Bluetooth para código de barras

---

### Fase 4 — Offline en Terminal POS

**Objetivo:** El terminal POS funciona durante cortes de conectividad y sincroniza al reconectarse.

**Alcance offline mínimo viable:**
- Catálogo de productos (sincronizado periódicamente cuando hay red)
- Creación de ventas locales con ID temporal
- Apertura de caja e impresión local (si impresora en misma red)
- Cola de sincronización al reconectar

**Fuera del alcance offline:**
- Facturación CFDI (requiere timbrado en tiempo real)
- Cambios de inventario (requieren validación centralizada)
- Reportes

**Conflictos de sincronización:**
- IDs temporales → IDs definitivos al sync
- Ventas offline se marcan con `OfflineOrigin = true` para auditoría
- Precios siempre se toman del servidor (último sync) — no editables offline

---

### Fase 5 — Báscula

**Objetivo:** Soporte para pesaje en el flujo de venta.

**Consideraciones:**
- Protocolos: RS-232 (más común en básculas comerciales MX), USB HID, red
- El agente local de la Fase 1 es el punto de integración natural
- Fabricantes a soportar: Toledo, Mettler, Torrey (los más comunes en MX)
- El navegador NO puede leer básculas de forma confiable — el agente es obligatorio

**Evaluación pendiente:** Definir modelo específico de báscula antes de diseñar protocolo.

---

## Posicionamiento de Mercado

Ver análisis completo: [`docs/01-Architecture/market-analysis-pos-mexico-2026.md`](market-analysis-pos-mexico-2026.md)

**Diferenciadores clave frente a la competencia:**
1. Precio justo y transparente (sin sorpresas de costo)
2. Offline-first en el terminal POS
3. CFDI simplificado (botón facturar, sin complejidad)
4. Hardware plug-and-play (agente local, sin configuración manual de drivers)
5. Soporte humano en español (WhatsApp + chat)
6. Tablet-optimized desde el diseño

---

## Principios de Diseño para las Fases Futuras

1. **Back-office y terminal son apps separadas** — comparten backend, no código de UI
2. **El agente local abstrae todo el hardware** — la web app solo hace HTTP a `localhost`
3. **Offline es incremental** — primero catálogo read-only, luego ventas, luego sync completo
4. **Multi-tenant antes de SaaS** — ningún cliente entra en producción sin tenant isolation
5. **El agente se actualiza independientemente** — versión semver propia, descarga automática

---

## Lo que NO cambia

- Stack actual (.NET 9, Blazor Server, MudBlazor, MySQL, EF Core)
- Arquitectura de capas (`App.Core`, `App.Services`, `App.Web`)
- Módulos ya implementados (CFDI, Facturas Globales, Cotizaciones, Remisiones, Etiquetas)
- Patrones de código (Result pattern, DbContext Factory, IStringLocalizer)
