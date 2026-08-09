# Guía de Despliegue White-Label (Multi-Tienda)

Esta guía documenta cómo desplegar el mismo código base para una tienda distinta a Cleeny, con su propio nombre, logo y colores, usando un contenedor MySQL compartido con una base de datos por tienda.

## Modelo de despliegue

- **Cleeny** vive en su propio VPS, con su propio MySQL — sin cambios, tal como ya operaba.
- **Un VDS aparte y dedicado** aloja Two Rockets y cualquier tienda nueva que siga, con **un solo contenedor MySQL compartido entre ellas** (una BD distinta por tienda dentro de ese mismo MySQL).
- **Un contenedor `App.Web` por tienda** en ese VDS, cada uno con su propia cadena de conexión (a su propia BD) y su propio perfil de marca.
- Reverse proxy (Traefik/Nginx) delante, ruteando por dominio/subdominio a cada contenedor.

No es multi-tenant de esquema compartido (no hay `TenantId` en las tablas) — es la misma app, desplegada N veces, cada una aislada en su propia BD. Ver [Roadmap SaaS POS](../01-Architecture/roadmap-saas-pos.md) para la evolución futura hacia multi-tenant real.

## Cómo operar el VDS de tiendas remotamente

En vez de entrar por SSH y clonar el repo en cada servidor, se usa un **Docker context** apuntando al daemon remoto — los comandos de `docker`/`docker compose` se corren desde tu máquina, parado en este repo local, y el build/ejecución ocurre en el servidor remoto:

```bash
# Una sola vez: crear el context (usa la llave SSH que ya tengas configurada para ese host)
docker context create tr --docker "host=ssh://usuario@ip-del-vds"

# Cada vez que quieras operar ese servidor:
docker context use tr
docker compose ...   # todo corre remoto, el .env y el docker-compose.yml se leen de tu carpeta local

# Para volver a tu Docker local:
docker context use default
```

**Requisito en el servidor remoto:** el usuario SSH debe estar en el grupo `docker` (si no, da `permission denied while trying to connect to the docker API`):
```bash
sudo usermod -aG docker <usuario>
# cerrar y volver a abrir la sesión SSH para que el grupo tome efecto
```

Con esto, no hace falta `git clone` ni `scp` del `.env.{tienda}` al servidor — todo se lee localmente y se envía sobre la conexión SSH del context.

### Perfiles de Compose — por qué son obligatorios, no opcionales

El servicio `db` **tiene** `profiles: [development, production, shared-db]` asignados a propósito (antes no tenía ninguno). Un servicio sin `profiles:` en Compose se incluye **siempre**, sin importar qué `--profile` se use — así que si `db` no tuviera perfil, cualquier `up` con `--profile tenant` lo arrastraría también, y como corre bajo un `-p` (nombre de proyecto) distinto al que lo levantó originalmente, Compose intentaría crear **una segunda copia** de `app-network` con el mismo subnet y fallaría con `Pool overlaps`.

Por eso, en el VDS de tiendas, **siempre hay que pasar el `--profile` correcto** (nunca correr `up -d` a secas):

```bash
# Bootstrap del MySQL de este VDS (solo la primera vez) — MYSQL_ROOT_PASSWORD vive en
# .env.{tienda}.secrets (secreto de infraestructura de BD, independiente de la app)
docker compose --profile shared-db --env-file .env.{tienda} --env-file .env.{tienda}.secrets up -d

# Levantar la app de una tienda (no necesita el .secrets — no toca MYSQL_ROOT_PASSWORD)
docker compose -p {tienda} --profile tenant --env-file .env.{tienda} up -d --build
```

## Dónde vive cada dato de marca

| Dato | Fuente | Editable en caliente | Notas |
|---|---|---|---|
| **Nombre de la tienda** | BD — `CompanySettings.CompanyName` | Sí, desde Ajustes → General | Sembrado una sola vez desde `Branding/{profile}.json` al primer arranque (`CompanyBrandingSeeder`). Sin fallback al JSON en lecturas posteriores — o está en BD, o se muestra vacío. |
| **Logo principal** (NavMenu, Login, PDFs de negocio) | BD — `CompanySettings.LogoBase64` | Sí, desde Ajustes → General | Usado por NavMenu, Login/ForgotPassword/ResetPassword, y por los PDFs (cotizaciones, remisiones, traspasos, conteos, reporte de ventas). Si no se ha subido nada, cae al archivo estático de `Branding/{profile}.json`. |
| **Logo de tickets** | BD — `TicketConfiguration.CompanyLogoBase64` | Sí, desde Ajustes → Tickets | **Campo separado** del logo principal — pensado para una variante simplificada/blanco y negro optimizada para impresión térmica. No se comparte con el logo principal. |
| **Colores del tema** (`PrimaryColor`/`SecondaryColor`) | `Branding/{profile}.json` | No — requiere redeploy | Fijos por deployment; `CurrentThemeService` es singleton y arma el `MudTheme` una sola vez al arrancar. |
| **Favicon** | `Branding/{profile}.json` → `FaviconPath` | No — requiere redeploy | Estático, leído por `AppRoot.razor` (`<link rel="icon">`). Archivo debe existir bajo `wwwroot`. |
| **`app_name` en emails/CFDI** (Factura Global, alertas de inventario, pre-factura de cotización) | BD — `CompanySettings.CompanyName`, con fallback a `Branding/{profile}.json` si no hay fila aún | Sí | Unificado con el nombre de tienda — si el admin lo cambia en Ajustes, también cambia ahí. |

**Por qué dos logos:** el logo de tickets impresos en térmica suele necesitar una versión más simple (blanco y negro, sin gradientes) que el logo de marca a color usado en pantalla y en PDFs. Son campos independientes en BD — subir uno no afecta al otro.

## Archivo de perfil de marca

Cada tienda tiene un archivo en `src/App.Web/Branding/{nombre}.json`:

```json
{
  "Application": {
    "Name": "Cleeny"
  },
  "Branding": {
    "LogoPath": "/images/brands/cleeny/logo.webp",
    "FaviconPath": "/images/brands/cleeny/favicon.ico",
    "PrimaryColor": "#1A6868",
    "SecondaryColor": "#7B3FA0"
  }
}
```

- `Application.Name` — nombre de marca por defecto. Solo se usa como **semilla inicial** de `CompanySettings.CompanyName` (una vez, al primer arranque) y como fallback si esa fila no existiera. Después de la siembra, el nombre real vive en BD y es editable desde Ajustes.
- `Branding.LogoPath` — ruta al **logo principal** de fallback (se usa solo hasta que se sube uno desde Ajustes → General). Debe existir bajo `wwwroot` (ver convención de carpetas abajo).
- `Branding.FaviconPath` — ruta al ícono de pestaña del navegador (`<link rel="icon">` en `AppRoot.razor`). A diferencia del logo, **no vive en BD** — es puramente estático, por deployment. Debe existir bajo `wwwroot`.
- `Branding.PrimaryColor` / `SecondaryColor` — colores base del tema MudBlazor. `CurrentThemeService` calcula automáticamente las variantes claras/oscuras (`Darken`/`Lighten`) mezclando el color con negro/blanco — no hace falta especificarlas a mano.

El perfil activo se selecciona con la variable de entorno `BRANDING_PROFILE` (por defecto `cleeny`), cargada en `Program.cs` antes de `ConfigureApplicationOptions`:

```csharp
var brandingProfile = builder.Configuration["BRANDING_PROFILE"] ?? "cleeny";
builder.Configuration.AddJsonFile(Path.Combine("Branding", $"{brandingProfile}.json"), optional: false, reloadOnChange: false);
```

El archivo se copia automáticamente al build (los `.json` bajo el proyecto son `Content` por defecto en el SDK web de .NET — no hace falta declararlo aparte en el `.csproj`).

## Convención de carpetas de assets estáticos

Los assets de fallback (logo principal + favicon) viven versionados en el repo, uno por tienda:

```
src/App.Web/wwwroot/images/brands/
  cleeny/
    logo.webp        ← logo principal de fallback (perfil "cleeny")
    favicon.ico
  tienda-x/
    logo.webp        ← logo principal de fallback (perfil "tienda-x")
    favicon.ico
```

En `Branding/tienda-x.json`, `LogoPath` y `FaviconPath` apuntan a `/images/brands/tienda-x/...`. El logo de fallback solo se usa hasta que el admin de esa tienda sube el suyo propio desde **Ajustes → General** (el cual queda en BD y tiene prioridad). El favicon no tiene equivalente en BD — siempre se sirve desde este archivo estático.

**Nota de compatibilidad:** `wwwroot/images/logo.webp` y `wwwroot/favicon.ico` (rutas planas, sin `brands/`) se mantienen como copias del logo/favicon de Cleeny — varios servicios de CFDI (`MexicoInvoiceService`, `GlobalInvoiceService`) todavía leen esa ruta hardcodeada directamente y no pasan por `BrandingOptions` (ver sección "Fuera de alcance"). No borrar esos archivos aunque parezcan duplicados.

## Onboarding de una tienda nueva — checklist

**Primera tienda en un VDS nuevo** (bootstrap del servidor, una sola vez):

1. Crear el context de Docker apuntando al VDS (ver sección anterior) y confirmar que el usuario SSH esté en el grupo `docker`.
2. Revisar qué subnet usa la red `bridge` por defecto en ese servidor: `docker network inspect bridge --format '{{range .IPAM.Config}}{{.Subnet}}{{end}}'`. Si coincide con `172.17.0.0/16` (el valor por defecto de Docker, distinto del VPS de Cleeny que ya fue reconfigurado), hay que darle a `app-network` un subnet libre — ver `APP_NETWORK_SUBNET`/`APP_NETWORK_GATEWAY` más abajo.
3. `docker network create app-shared-network` (red externa, una sola vez por servidor).
4. Crear `.env.{tienda}.secrets` (copiar `.env.production.secrets.example`) con `MYSQL_ROOT_PASSWORD`.
5. `docker compose --profile shared-db --env-file .env.{tienda} --env-file .env.{tienda}.secrets up -d` — levanta el MySQL de este VDS y crea automáticamente la BD/usuario de la primera tienda (vía `MYSQL_DATABASE`/`MYSQL_USER`/`MYSQL_PASSWORD` del `.env`, y `MYSQL_ROOT_PASSWORD` del `.secrets`).

**Cada tienda nueva** (incluida la primera, después del bootstrap):

1. **Crear el perfil de marca**: `src/App.Web/Branding/{tienda}.json` con `Application.Name`, `Branding.LogoPath`, `Branding.FaviconPath`, `Branding.PrimaryColor`, `Branding.SecondaryColor`.
2. **Agregar el logo de fallback y el favicon** (recomendado): `wwwroot/images/brands/{tienda}/logo.webp` y `wwwroot/images/brands/{tienda}/favicon.ico`.
3. **Si NO es la primera tienda en este servidor**, crear su BD manualmente en el MySQL ya corriendo:
   ```bash
   docker exec -it {container-prefix}-mysql mysql -uroot -p -e "CREATE DATABASE {tienda}; CREATE USER '{tienda}'@'%' IDENTIFIED BY '...'; GRANT ALL PRIVILEGES ON {tienda}.* TO '{tienda}'@'%'; FLUSH PRIVILEGES;"
   ```
4. **Crear `.env.{tienda}`** (copiar `.env.tenant.example`) con `BRANDING_PROFILE`, `DATABASE_CONNECTION_STRING`, `Application__BaseUrl`, `PORT` (único por tienda), y límites de recursos.
5. **Levantar la app**: `docker compose -p {tienda} --profile tenant --env-file .env.{tienda} up -d --build` — nunca omitir `--profile tenant` (ver por qué en la sección anterior). Las migraciones de EF Core y `CompanyBrandingSeeder` corren solos al arrancar.
6. **Configurar el reverse proxy** para rutear el dominio/subdominio de la tienda al puerto de este contenedor.
7. Verificar logs: `docker compose -p {tienda} logs -f webapp-tenant`.
8. Primer login: `admin` / `Admin123!` (semilla por defecto, igual en toda BD nueva) — **cambiarla de inmediato**.
9. El admin de la tienda puede después ajustar nombre y logo principal (Ajustes → General) y logo de tickets (Ajustes → Tickets) sin redeploy.

## Variables de entorno relevantes

```bash
# Brand identity — selecciona qué Branding/{profile}.json cargar
BRANDING_PROFILE=cleeny

# El resto de Application__* sigue siendo config de infraestructura, no de marca
Application__Version=1.0.0
Application__DefaultLanguage=es-MX
Application__SupportedLanguages__0=es-MX
Application__SupportedLanguages__1=en-US
Application__BaseUrl=https://sistema.cleeny.com.mx

# Solo necesario en un VDS de tiendas si el subnet por defecto (172.17.100.0/24) choca
# con el `bridge` u otra red ya existente en ese servidor (ver checklist de bootstrap):
APP_NETWORK_SUBNET=172.19.100.0/24
APP_NETWORK_GATEWAY=172.19.100.1

# Límites de recursos del MySQL compartido (antes sin límite — ver docker-compose.yml)
DB_CPU_LIMIT=2.0
DB_MEMORY_LIMIT=2G
DB_CPU_RESERVATION=1
DB_MEMORY_RESERVATION=1G
```

## Fuera de alcance (no tocado por este sistema)

- `MexicoInvoiceService` (CFDI individual) sigue leyendo el logo desde archivo estático `images/logo.webp` directo — no pasa por `BrandingOptions` ni por el logo en BD. No se tocó para no arriesgar el flujo de timbrado.
- `wwwroot/EmailTemplates/welcome.html` y `password-reset.es.html` tienen "Cleeny" literal (no tokenizado) — a diferencia de los templates de producción en `App.Services/Resources/EmailTemplates/*` que sí usan `{{ app_name }}`/`{{ company_logo_url }}`.
