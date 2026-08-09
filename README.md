# AppBase

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![EF Core](https://img.shields.io/badge/EF%20Core-9.0-purple)](https://docs.microsoft.com/ef/core)
[![MySql](https://img.shields.io/badge/MySQL-8.0-00758F)](https://www.mysql.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Docker](https://img.shields.io/badge/Docker-20.10%2B-2496ED)](https://www.docker.com/)

Plantilla base genérica y reutilizable para nuevos proyectos .NET 9 Blazor Server, con N-Layer architecture y MySQL/EF Core.

## Qué incluye

- **Identity**: usuarios, roles, permisos granulares (claims-based authorization)
- **Soft delete**: `ISoftDelete` + query filters globales
- **Auditoría**: `AuditLogInterceptor` (bitácora de cambios) + `[SensitiveData]` (redacción de campos sensibles) + visor admin en `/admin/audit-log`
- **Fecha/hora**: `IDateTime`/`DateTimeService`, todo en UTC internamente
- **Current user**: `ICurrentUserService` para acceso async al usuario autenticado
- **Branding / white-label**: perfil de marca (`Branding/{profile}.json`, variable `BRANDING_PROFILE`) — nombre, logo, colores por variable de entorno
- **Email**: envío (MailKit) + gestión de plantillas HTML/CSS editables desde UI, con presets (classic/compact/modern)
- **PDF**: generación genérica HTML/Razor View → PDF vía PuppeteerSharp
- **Manejo de archivos/imágenes**: upload, thumbnails, `IImageService`
- **Result<T> pattern** para todos los métodos de servicio
- **DbContext factory pattern** con interceptors de auditoría

Ver [`CLAUDE.md`](CLAUDE.md) para convenciones de código y patrones detallados.

## Estructura del Proyecto

```
AppBase/
├── src/
│   ├── App.Core/              # Contratos base e interfaces
│   ├── App.Models/            # Entidades y modelos
│   ├── App.Models.Data/       # Acceso a datos y EF Core
│   ├── App.Services/          # Lógica de negocio
│   ├── App.Shared/            # Utilidades compartidas
│   └── App.Web/               # Aplicación Blazor
├── tests/
│   └── App.Services.Tests/    # Tests de servicios (NUnit)
├── docs/
│   ├── 01-Architecture/       # ADRs y diagramas
│   └── 02-Development/        # Guías de desarrollo
├── .dockerfile                # Dockerfile principal
├── docker-compose.yml         # Configuración de servicios
├── .env.development.example   # Template variables desarrollo
└── .env.production.example    # Template variables producción
```

## Tecnologías

- **.NET 9**
  - ASP.NET Core 9
  - Entity Framework Core 9
  - Identity Framework
- **Blazor Server**
  - Server-Side Rendering
  - SignalR para comunicación en tiempo real
- **MudBlazor**
  - Componentes Material Design
  - Temas personalizables
- **MySQL 8**
- **Docker**
- **Serilog** para logging

## Docker

### Prerrequisitos
- Docker Desktop 20.10 o superior
- Docker Compose v2
- .NET SDK 9.0 (para desarrollo local)

### Archivos de Configuración

#### Variables de Entorno
El proyecto utiliza archivos .env para cada ambiente. Estos archivos están ignorados en git por seguridad y deben ser creados localmente:

1. Para Desarrollo:
```bash
# Copiar template
cp .env.development.example .env.development

# Editar con tus valores
notepad .env.development  # Windows
nano .env.development     # Linux/Mac
```

2. Para Producción:
```bash
# Copiar template
cp .env.production.example .env.production

# Editar con tus valores
notepad .env.production  # Windows
nano .env.production     # Linux/Mac
```

> **Importante:**
> - Nunca comitear los archivos `.env.development` y `.env.production` al repositorio
> - Siempre mantener actualizados los archivos `.example` con la estructura correcta
> - Cambiar todas las contraseñas por defecto
> - En producción, usar valores seguros y únicos para todas las variables

### Ambientes y Comandos

#### Desarrollo

```bash
# Construir imágenes
docker compose --profile development --env-file .\.env.development --env-file .\.env.development.secrets build --no-cache

# Iniciar servicios
docker compose --profile development --env-file .\.env.development --env-file .\.env.development.secrets up -d

# Ver logs
docker compose --profile development logs -f

# Detener servicios
docker compose --profile development --env-file .\.env.development --env-file .\.env.development.secrets down
```

> `MYSQL_ROOT_PASSWORD` vive en `.env.development.secrets` (nunca en git) — pasa siempre ambos `--env-file` juntos.

#### Producción

```bash
docker compose --profile production --env-file .env.production --env-file .env.production.secrets build --no-cache
docker compose --profile production --env-file .env.production --env-file .env.production.secrets up -d
docker compose --profile production logs -f
docker compose --profile production --env-file .env.production --env-file .env.production.secrets down
```

> `MYSQL_ROOT_PASSWORD` vive en `.env.production.secrets` (nunca en git) — pasa siempre ambos `--env-file` juntos.

### Servicios y Puertos

| Servicio | Desarrollo | Producción |
|----------|------------|------------|
| WebApp   | 8080       | 8080       |
| MySQL*   | 3306       | N/A        |

> *MySQL solo se incluye para desarrollo y pruebas. En producción, se recomienda usar un servicio de base de datos administrado (AWS RDS, Azure Database for MySQL, Google Cloud SQL, etc.).

### Healthchecks

- **WebApp:** `/health` — intervalo 30s, timeout 30s, 3 reintentos
- **MySQL:** `mysqladmin ping` — intervalo 10s, timeout 5s, 5 reintentos

### Servidor con CSF Firewall (despliegue en VPS)

Si despliegas en un VPS con **CSF (ConfigServer Security Firewall)**, este administra iptables y puede interferir con Docker:

1. En `/etc/csf/csf.conf`: `DOCKER = "1"`, `DOCKER_DEVICE = "docker0"`, `DOCKER_NETWORK4 = "172.17.0.0/16"`.
2. En `/etc/docker/daemon.json`, ajustar `bip`/`default-address-pools` para que las redes de Docker Compose caigan dentro del rango permitido en CSF, y `"userland-proxy": false` (sin esto CSF bloquea `docker-proxy` en OUTPUT, causando 502).
3. Reiniciar: `systemctl restart docker && csf -r`.
4. Para desplegar sin conectarte manualmente por SSH cada vez, usa un contexto Docker remoto: `docker context create <nombre> --docker "host=ssh://user@servidor"`, y antepón `docker --context <nombre>` a los comandos de compose.

## Base de Datos

### Migrations

```bash
# Crear una nueva migración
dotnet ef migrations add MigrationName --project src/App.Models.Data --startup-project src/App.Web

# Aplicar migraciones
dotnet ef database update --project src/App.Models.Data --startup-project src/App.Web
```

> Requiere `dotnet tool install --global dotnet-ef` y una cadena de conexión válida en `appsettings.Development.json`.

## Inicio Rápido

### Desarrollo con Docker

```bash
git clone <tu-nuevo-repo>
cd AppBase
cp .env.development.example .env.development
cp .env.development.secrets.example .env.development.secrets
# Editar ambos archivos con tus valores (.secrets nunca se commitea)
docker compose --profile development --env-file .\.env.development --env-file .\.env.development.secrets up -d
```

Acceder a http://localhost:8080. Usuario sembrado por defecto: `admin` / `Admin123!` (rol `SuperAdmin`).

### Desarrollo sin Docker

1. Configurar `appsettings.Development.json` con tu cadena de conexión a MySQL.
2. Crear base de datos local: `CREATE DATABASE AppBase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`
3. `dotnet run --project src/App.Web`

### Manejo de Zonas Horarias

- Todas las fechas y horas se almacenan en **UTC** en la base de datos
- Los timestamps de auditoría (`CreatedAt`, `ModifiedAt`) siempre están en UTC
- Los contenedores Docker usan `TZ=UTC` por defecto

## Seguridad

- Autenticación Identity Framework
- Autorización basada en roles y claims granulares
- Auditoría automática
- Soft delete
- Logging comprehensivo (Serilog)
- HTTPS forzado en producción

## Cómo empezar un proyecto nuevo desde esta base

1. Clona este repo con un nombre nuevo.
2. Renombra el proyecto/solución si aplica (`App.sln` → `TuApp.sln`, namespaces si quieres cambiarlos).
3. Reemplaza `Branding/default.json` con el nombre/colores/logo de tu proyecto.
4. Agrega tus propios módulos de dominio (entidades en `App.Models`, DTOs en `App.Core`, servicios en `App.Services`, páginas en `App.Web`).
5. Corre `dotnet ef migrations add YourFirstFeature` una vez que agregues tus propias entidades.

## Licencia

Uso interno / privado.
