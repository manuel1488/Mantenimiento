# Clasificación de Clave de Producto/Servicio SAT

Guía para clasificar correctamente un `Servicio` nuevo (o revisar uno existente) contra el catálogo oficial del SAT `c_ClaveProdServ`, usado para CFDI al facturar. También cubre cómo actualizar el catálogo local si el SAT publica cambios.

## Estructura del código (8 dígitos)

El código de Producto/Servicio se arma en niveles anidados de 2 dígitos cada uno. Los primeros N dígitos identifican el nivel superior:

| Nivel | Dígitos usados | Ejemplo | Nombre en el ejemplo |
|---|---|---|---|
| Segmento | 1–2 | `10` | Material Vivo Vegetal y Animal, Accesorios y Suministros |
| Familia | 1–4 | `1010` | Animales vivos |
| Clase | 1–6 (siempre termina en `00`) | `101015` | Animales vivos de granja |
| Producto/Servicio (código final, el que se usa en el CFDI) | 1–8 | `10101501` | Gatos vivos |

Adicionalmente existe un nivel **Tipo** (`1 = Productos`, `2 = Servicios`) que agrupa Segmentos, pero **no forma parte del código de 8 dígitos** — es solo un filtro de navegación (igual que en el sitio oficial del SAT).

## Cómo elegir la clave para un Servicio nuevo

En `ServicioDialog` (`/gestion/servicios`), el campo "Clave de Producto/Servicio SAT" ofrece dos formas de buscar:

1. **Búsqueda directa** (por defecto): escribe una palabra clave o el código si ya lo conoces — busca por texto en todo el catálogo (código o descripción).
2. **Asistente por categoría** (botón junto al campo, ícono de brújula): si no sabes el código, abre un modal que reproduce el flujo del sitio oficial del SAT (`pys.sat.gob.mx`) — eliges **Tipo → Segmento → Familia → Clase** en cascada y al dar "Buscar" se listan los Producto/Servicio finales de esa Clase para elegir uno.

Regla general (igual que documenta el SAT y guías como la de Aspel): siempre se debe usar el código más específico posible que describa lo que realmente se está facturando — evita quedarte en un código de Clase o Familia genérico si existe un Producto/Servicio más preciso.

## Dónde vive el catálogo (para mantenimiento futuro)

Todo se seedea desde CSV en `src/App.Web/Data/FiscalCatalogs/` vía `FiscalCatalogSeeder` (`src/App.Services/Seeders/FiscalCatalogSeeder.cs`), el mismo mecanismo que los catálogos de Régimen Fiscal / Uso de CFDI / Unidad de Medida SAT:

| Archivo CSV | Tabla | Contenido |
|---|---|---|
| `claves_prod_serv_sat.csv` | `cat_claves_prod_serv_sat` | Los 52,514 códigos oficiales completos (nivel Clase y nivel Producto/Servicio, código + descripción) |
| `tipos_prod_serv_sat.csv` | `cat_tipos_prod_serv_sat` | 2 filas: Productos / Servicios |
| `segmentos_prod_serv_sat.csv` | `cat_segmentos_prod_serv_sat` | 57 Segmentos con nombre oficial |
| `familias_prod_serv_sat.csv` | `cat_familias_prod_serv_sat` | 421 Familias con nombre oficial |

**Fuentes originales de estos datos** (por si el SAT publica una actualización y hay que regenerar los CSV):
- `claves_prod_serv_sat.csv` — copiado del catálogo oficial ya procesado en el repo hermano `C:\repos\Cleeny` (`src/App.Web/Data/FiscalCatalogs/product_services.csv`), que a su vez viene del Anexo 20 del SAT.
- `tipos_prod_serv_sat.csv` / `segmentos_prod_serv_sat.csv` / `familias_prod_serv_sat.csv` — el catálogo oficial plano del SAT **no incluye nombres** para los niveles Tipo/Segmento/Familia, solo para Clase y Producto/Servicio. Estos tres archivos se generaron aplanando `data/pys.json` del repositorio público **[phpcfdi/resources-sat-pys](https://github.com/phpcfdi/resources-sat-pys)** (licencia Unlicense/dominio público), que sí mantiene esa jerarquía con nombres oficiales.

### Cómo regenerar los CSV si el SAT publica una actualización

1. Descargar/actualizar `data/pys.json` desde `phpcfdi/resources-sat-pys` (o el archivo Excel/CSV vigente del Anexo 20 del SAT para `claves_prod_serv_sat.csv`).
2. Aplanar la jerarquía JSON a los 3 CSV de Tipo/Segmento/Familia (mismo formato de columnas: `code,description` y `code,description,tipo_code`/`segmento_code`).
3. Reemplazar los archivos en `src/App.Web/Data/FiscalCatalogs/`.
4. **Importante**: `FiscalCatalogSeeder` es idempotente vía un chequeo `AnyAsync()` — si la tabla ya tiene filas, el seeder NO vuelve a insertar ni actualiza nada. Para forzar una recarga con datos nuevos hay que vaciar la tabla correspondiente manualmente antes de reiniciar la app (o escribir una migración de datos ad hoc), no basta con reemplazar el CSV y reiniciar.

## Catálogo relacionado: Unidad de Medida

La Unidad de Medida de un Servicio sigue el mismo patrón (catálogo propio pequeño en `UnidadMedida` + catálogo oficial completo `ClaveUnidadSatCatalogo` de referencia) — ver el código de `UnidadMedidaSatLinker` (`src/App.Services/Seeders/UnidadMedidaSatLinker.cs`) como precedente si se necesita un mecanismo similar de vinculación automática para Producto/Servicio en el futuro.
