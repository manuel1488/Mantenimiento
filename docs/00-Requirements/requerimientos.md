# Requerimientos del Sistema — Gestión de Obras y Servicios de Mantenimiento

> Estado: **Borrador para validación** — producto de entrevista de levantamiento de requerimientos + mockups de referencia (Excel/wireframes proporcionados).
> Última actualización: 2026-08-09.

## 1. Contexto del negocio

La empresa presta **servicios de construcción/remodelación y mantenimiento a clientes externos** (no es mantenimiento interno de sus propias instalaciones). El trabajo se organiza en **Obras/Proyectos**, cada una compuesta por una o varias **Actividades** (ej. Tablaroca, Pintura, Impermeabilizante, Yeso), medidas y cotizadas por **unidad de medida fija del catálogo (m², m³, pieza, etc.)**. También existen solicitudes de mantenimiento correctivo simples, que se modelan igual — como una Obra con una sola Actividad.

El trabajo se origina como **solicitudes puntuales** (sin contratos recurrentes por ahora), pasa por **cotización con tarifario fijo por unidad**, aprobación del cliente, ejecución en campo (con **técnicos propios o subcontratistas**), seguimiento de avance por actividad, y **facturación con CFDI**. El cobro es de **pago inmediato/anticipo**.

**Diseño mobile-first**: tanto la operación del Técnico en campo como la vista de seguimiento del Cliente deben funcionar bien en celular — es el dispositivo principal de uso para ambos.

## 2. Actores (roles)

| Rol | Descripción |
|---|---|
| **Administrador** | Control total: catálogo de Servicios (con unidad, precio y rendimiento), usuarios, reportes globales. |
| **Coordinador/Despachador** | Recibe solicitudes, crea Obras y sus Actividades, cotiza, registra aprobación del cliente, asigna técnicos/subcontratistas, da seguimiento al avance, registra facturación. |
| **Técnico de campo** | Consulta sus Actividades asignadas (desde celular), registra % de avance, sube evidencia fotográfica antes/después, cierra la actividad. |
| **Cliente** | No tiene cuenta ni login. Recibe una **liga de solo lectura con URL codificada** (token simple, no adivinable) para ver el avance de su Obra. No aprueba ni interactúa dentro del sistema — todo eso lo registra el Coordinador. |

## 3. Entidades y catálogos principales

- **Cliente**: Datos Comerciales (Nombre/Nombre Comercial, País, Teléfono, Nombre de Contacto, Correo, Dirección Comercial: Calle, Núm. Ext/Int, Colonia, Ciudad, Estado, CP) + Datos Fiscales (RFC, Razón Social, Régimen Fiscal, CP fiscal, Uso de CFDI — necesarios para timbrado). **No existe catálogo de Sitios/sucursales** — la dirección del Cliente es la fiscal/comercial única; la ubicación específica de cada Obra se captura libremente al crear la Obra/Cotización, sin reutilizarse como catálogo.
- **Servicio (catálogo)**: ID, nombre, descripción, **unidad de medida fija** (m², m³, pieza, etc.), **precio unitario**, y **rendimiento** (días estimados por unidad, para calcular automáticamente el tiempo estimado de una Actividad — editable/personalizable caso por caso).
- **Obra/Proyecto**: entidad que agrupa una o varias Actividades para un Cliente. Tiene estado general (ver §4), dirección de la obra (texto libre, no catálogo), fecha de solicitud, y es la unidad que se cotiza, aprueba y factura en conjunto.
- **Actividad** (línea de la Obra): Servicio del catálogo + cantidad (m²/m³/etc.) + costo (precio unitario × cantidad, ajustable) + tiempo estimado (calculado, editable) + técnico/subcontratista asignado + **% de avance individual** + fecha de inicio de obra de esa actividad + evidencia fotográfica (antes / después, múltiples fotos) + estado de avance propio.
- **Técnico**: personal interno, con especialidad(es) implícita(s) por los Servicios que puede ejecutar.
- **Subcontratista**: proveedor externo equivalente al Técnico para efectos de asignación.
- **Cotización**: documento generado a partir de las Actividades de una Obra (Servicio + cantidad + precio unitario por línea, con total).
- **Factura**: documento CFDI ligado a una Obra facturada.

## 4. Ciclo de vida de la Obra

Estado general de la Obra (nivel obra, no actividad):

```
Solicitada → Cotizada → Aprobada → En proceso → Finalizada → Facturada
                 ↓
             Rechazada
```

- La Obra pasa a `En proceso` cuando al menos una Actividad inicia.
- La Obra pasa a `Finalizada` cuando **todas** sus Actividades están finalizadas (100% avance cada una).
- **Flujo de urgencia**: una Obra puede marcarse urgente y saltar de `Solicitada` a `En proceso` sin cotización previa; se cotiza después de finalizar y sigue el mismo flujo de aprobación antes de facturar.
- **Rechazo**: desde `Cotizada`, el Coordinador puede marcar la cotización como `Rechazada` si el cliente no aprueba. Una Obra `Rechazada` puede reactivarse generando una nueva cotización (vuelve a `Cotizada`) si el cliente cambia de opinión.

### Ciclo de vida de la Actividad (dentro de una Obra `En proceso`)

Cada Actividad tiene su propio seguimiento independiente:
- Fecha de inicio de obra (de esa actividad).
- % de avance (0–100, actualizado por el Técnico).
- Evidencia fotográfica antes/después.
- Se marca individualmente como finalizada.

## 5. Casos de uso

### CU-01: Registrar Obra/solicitud
**Actor:** Coordinador
**Flujo:**
1. Selecciona o crea el Cliente.
2. Captura la dirección de la obra (texto libre) y si es **urgente**.
3. El sistema crea la Obra en estado `Solicitada`.

### CU-02: Agregar actividades y cotizar
**Actor:** Coordinador
**Flujo:**
1. Sobre la Obra, agrega una o más líneas: selecciona Servicio del catálogo, captura cantidad (según unidad del servicio: m², m³, etc.).
2. El sistema calcula costo (precio unitario del catálogo × cantidad, ajustable) y tiempo estimado (rendimiento del catálogo × cantidad, ajustable).
3. El Coordinador repite para cada Actividad adicional (botón "Agregar").
4. Genera el documento de Cotización (botón "Cotización") — la Obra pasa a `Cotizada`.

### CU-03: Registrar aprobación de cotización
**Actor:** Coordinador
**Flujo:**
1. El cliente aprueba fuera del sistema (teléfono/correo/WhatsApp).
2. El Coordinador marca la cotización como **Aprobada**, indicando el medio de aprobación (nota libre) — la Obra pasa a `Aprobada`.

### CU-04: Asignar técnico o subcontratista por actividad
**Actor:** Coordinador
**Flujo:**
1. Para cada Actividad de la Obra, el Coordinador asigna manualmente un Técnico o Subcontratista.
2. Al iniciar la primera actividad, el sistema **envía un correo automático al Cliente** avisando el arranque de la obra.

### CU-05: Ejecutar y dar seguimiento a una actividad (Técnico)
**Actor:** Técnico de campo (desde celular)
**Flujo:**
1. Consulta sus Actividades asignadas.
2. Sube foto de evidencia **antes** de iniciar.
3. Actualiza % de avance conforme progresa (una o varias veces).
4. Sube foto de evidencia **después** al terminar.
5. Marca la Actividad como Finalizada.
**Postcondición:** cuando todas las Actividades de la Obra están finalizadas, la Obra pasa a `Finalizada`.

### CU-06: Facturar Obra
**Actor:** Coordinador
**Precondición:** Obra `Finalizada` y cotización `Aprobada`.
**Flujo:**
1. Genera la factura CFDI a partir de las líneas cotizadas (timbrado real fuera del MVP — ver §7).
2. Registra el pago (inmediato/anticipo).
3. La Obra pasa a `Facturada`.

### CU-07: Administrar catálogo de Servicios
**Actor:** Administrador
**Flujo:** Alta/edición/baja de Servicios: nombre, descripción, unidad de medida, precio unitario, rendimiento (días por unidad).

### CU-09: Reasignar técnico o subcontratista de una actividad
**Actor:** Coordinador
**Descripción:** Cuando el asignado original no puede continuar con la Actividad.
**Flujo:**
1. El Coordinador selecciona la Actividad y elige un nuevo Técnico/Subcontratista.
2. Captura el motivo de la reasignación (nota obligatoria).
3. El sistema conserva un historial de reasignaciones de la Actividad (asignado anterior, nuevo asignado, motivo, fecha).

### CU-08: Consultar avance (vista de Cliente, solo lectura)
**Actor:** Cliente (sin login)
**Flujo:**
1. El Cliente abre una liga con un token único y no adivinable asociado a su Obra.
2. Ve: lista de Actividades, % de avance de cada una, evidencia fotográfica antes/después.
3. No puede editar, aprobar ni comentar nada — es de solo lectura.

## 6. Reglas de negocio

| ID | Regla |
|---|---|
| RN-01 | Toda Obra debe estar ligada a un Cliente; la dirección de la obra se captura libremente por Obra, sin catálogo de sitios. |
| RN-02 | Cada Servicio del catálogo tiene unidad de medida y precio unitario fijos; el costo de una Actividad es cantidad × precio unitario, ajustable manualmente por el Coordinador. |
| RN-03 | El tiempo estimado de una Actividad se calcula por defecto desde el rendimiento del catálogo (días por unidad × cantidad), pero puede personalizarse caso por caso. |
| RN-04 | Una Obra marcada **urgente** puede iniciar ejecución sin cotización aprobada previa; se cotiza al finalizar y requiere aprobación registrada antes de facturar. |
| RN-05 | Una Obra solo puede facturarse si su cotización está `Aprobada` y la Obra está `Finalizada` (todas sus Actividades al 100%). |
| RN-06 | La aprobación de una cotización debe registrar: quién la marcó, fecha/hora y el medio por el cual el cliente aprobó. |
| RN-07 | El % de avance y las fechas de inicio/fin se manejan **por Actividad**, no a nivel Obra; el estado de la Obra se deriva del conjunto de sus Actividades. |
| RN-08 | Se envía correo automático al Cliente únicamente al iniciar la primera Actividad de la Obra (arranque de obra) — no hay otras notificaciones automáticas en el MVP. |
| RN-09 | No se controla stock/inventario de materiales en el MVP. |
| RN-10 | El pago se registra como inmediato/anticipo — sin manejo de crédito a plazo en el MVP. |
| RN-11 | La vista de Cliente es de solo lectura, accesible mediante URL con token simple (no requiere cuenta ni contraseña) y no expone acciones de edición; el token **no expira** — permanece accesible incluso después de que la Obra se factura. |
| RN-12 | Toda reasignación de Técnico/Subcontratista en una Actividad requiere capturar un motivo, conservado como historial de esa Actividad. |
| RN-13 | Cualquier Coordinador puede ver y gestionar cualquier Obra — no hay distribución de carga ni restricción por coordinador asignado. |
| RN-14 | El tarifario de Servicios es único para todos los Clientes en el MVP; no existen descuentos ni precios especiales por cliente. |

## 7. Alcance del MVP vs. fases futuras

**Dentro del MVP:**
- Catálogo de Servicios (unidad de medida, precio, rendimiento).
- Gestión de Clientes (datos comerciales + fiscales).
- Obra con múltiples Actividades; cotización, aprobación manual, asignación manual de Técnico/Subcontratista por actividad.
- Seguimiento de avance por Actividad (% avance, fechas, evidencia foto antes/después).
- Vista de Cliente de solo lectura vía URL con token.
- Correo automático único al iniciar obra.
- Registro de factura (documento) y pago inmediato.
- Diseño **mobile-first** para Técnico y vista de Cliente.
- Roles: Administrador, Coordinador, Técnico.

**Explícitamente fuera del MVP (fases futuras):**
- Portal de Cliente con cuenta/login, aprobación de cotización en línea.
- Mensajería/chat entre Coordinador y Cliente.
- Alertas automáticas (retraso de actividad vs. tiempo estimado).
- Integración real de timbrado CFDI con un PAC.
- App móvil nativa (se usa navegador web responsive).
- Control de inventario/stock de materiales.
- Contratos de mantenimiento preventivo recurrente con calendario.
- SLAs / tiempos de respuesta por prioridad.

## 8. Requerimientos no funcionales

- **.NET 9 Blazor Server**, MudBlazor, MySQL, arquitectura N-Layer (ver `CLAUDE.md` raíz del proyecto).
- **Mobile-first**: las pantallas de Técnico (captura de avance/fotos) y la vista de Cliente deben probarse y funcionar correctamente en viewport de celular como caso primario, no solo como adaptación de escritorio.
- Todo el código en inglés; textos de UI en español vía `IStringLocalizer`.
- Auditoría (creado/modificado por y fecha) y soft delete en entidades de negocio (Cliente, Obra, Actividad, Cotización, Factura, Servicio).
- Autorización basada en Claims por rol (Administrador, Coordinador, Técnico); la vista de Cliente es una ruta pública protegida solo por el token, sin autenticación de usuario.
- Manejo de archivos: fotos de evidencia (antes/después) por Actividad, considerar límite de tamaño/formato y almacenamiento organizado por Obra/Actividad.
