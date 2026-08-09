# Cleeny

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![EF Core](https://img.shields.io/badge/EF%20Core-9.0-purple)](https://docs.microsoft.com/ef/core)
[![MySql](https://img.shields.io/badge/MySQL-8.0-00758F)](https://www.mysql.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Docker](https://img.shields.io/badge/Docker-20.10%2B-2496ED)](https://www.docker.com/)

Sistema de gestión de ventas e inventario para tienda de productos de limpieza, construido con .NET 9.

## Estado del Proyecto

Este proyecto representa la fase inicial de implementación con una arquitectura N-Layer. La estructura actual sirve como base para una futura migración hacia Clean Architecture.

## Características Planificadas

- **Gestión de Tienda** (`/tienda`)
  - Control de inventario de productos de limpieza
  - Catálogo de productos (detergentes, desinfectantes, jabones, etc.)
  - Ventas y facturación
  - Facturación electrónica (CFDI)

- **Características Base**
  - Sistema de autenticación y autorización
  - Auditoría y soft delete
  - Gestión de archivos
  - Configuración flexible
  - Multi-idioma (i18n)
  - Temas personalizables

## Estructura del Proyecto

```
Cleeny/
├── src/
│   ├── App.Core/              # Contratos base e interfaces
│   ├── App.Models/            # Entidades y modelos
│   ├── App.Models.Data/       # Acceso a datos y EF Core
│   ├── App.Services/          # Lógica de negocio
│   ├── App.Shared/            # Utilidades compartidas
│   └── App.Web/               # Aplicación Blazor
├── mysql/
│   └── init/
│       └── 01-init.sql        # Script de inicialización
├── docs/
│   ├── architecture/          # Documentación de arquitectura
│   └── guides/                # Guías de usuario y desarrollo
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

### Inicialización de MySQL

El archivo `mysql/init/01-init.sql` se ejecuta automáticamente cuando el contenedor de MySQL se inicia por primera vez.

**Notas importantes:**
- El script solo se ejecuta en la primera inicialización del volumen
- Si se elimina el volumen (`docker compose down -v`), el script se ejecutará nuevamente
- Las credenciales en el script deben coincidir con las variables de entorno
- Para múltiples scripts, se ejecutan en orden alfabético (01-, 02-, etc.)

### Configuración del Servidor (VPS Neubox con CSF Firewall)

El servidor de producción usa **CSF (ConfigServer Security Firewall)** que administra iptables y puede interferir con Docker. Para que la app funcione correctamente se requieren dos configuraciones en el servidor:

#### 1. Habilitar soporte Docker en CSF

En `/etc/csf/csf.conf` cambiar (por defecto viene en `0`):

```
DOCKER = "1"
DOCKER_DEVICE = "docker0"
DOCKER_NETWORK4 = "172.17.0.0/16"
```

#### 2. Configurar Docker daemon

Crear o editar `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "bip": "172.26.0.1/16",
  "default-address-pools": [
    {"base": "172.17.0.0/16", "size": 24}
  ],
  "userland-proxy": false
}
```

**Por qué cada opción:**

| Opción | Razón |
|--------|-------|
| `bip` | Mueve la red interna de `docker0` a `172.26.0.0/16`, liberando `172.17.0.0/16` para user-defined networks |
| `default-address-pools` | Fuerza que `app-network` use subnets dentro de `172.17.0.0/16`, rango que CSF tiene permitido en `DOCKER_NETWORK4` |
| `userland-proxy: false` | Sin esto, CSF bloquea el proceso `docker-proxy` en la cadena OUTPUT causando 502. Con `false`, el tráfico usa iptables directamente |

Después de aplicar la configuración, reiniciar ambos servicios:

```bash
systemctl restart docker
csf -r
```

#### 3. Contexto Docker Remoto (SSH)

Para desplegar desde tu máquina local sin conectarse manualmente al servidor:

```bash
# Crear el contexto una sola vez
docker context create cleeny --docker "host=ssh://user@servidor"

# Construir imagen en el servidor remoto
docker --context cleeny compose --profile production --env-file .env.production --env-file .env.production.secrets build --no-cache

# Desplegar
docker --context cleeny compose --profile production --env-file .env.production --env-file .env.production.secrets up -d

# Ver logs
docker --context cleeny compose --profile production logs -f
```

> `MYSQL_ROOT_PASSWORD` vive en `.env.production.secrets` (nunca en git) — pasa siempre ambos `--env-file` juntos.

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

# Limpiar volúmenes (borra datos)
docker compose --profile development --env-file .\.env.development --env-file .\.env.development.secrets down -v
```

> `MYSQL_ROOT_PASSWORD` vive en `.env.development.secrets` (nunca en git) — pasa siempre ambos `--env-file` juntos.

#### Producción

```bash
# Construir imágenes (en servidor remoto vía SSH context)
docker --context cleeny compose --profile production --env-file .env.production --env-file .env.production.secrets build --no-cache

# Iniciar servicios
docker --context cleeny compose --profile production --env-file .env.production --env-file .env.production.secrets up -d

# Ver logs
docker --context cleeny compose --profile production logs -f

# Detener servicios
docker --context cleeny compose --profile production --env-file .env.production --env-file .env.production.secrets down
```

> `MYSQL_ROOT_PASSWORD` vive en `.env.production.secrets` (nunca en git) — pasa siempre ambos `--env-file` juntos.

### Servicios y Puertos

| Servicio | Desarrollo | Producción |
|----------|------------|------------|
| WebApp   | 8080       | 8080       |
| MySQL*   | 3306       | N/A        |

> *MySQL solo se incluye para desarrollo y pruebas. En producción, se recomienda usar un servicio de base de datos administrado (AWS RDS, Azure Database for MySQL, Google Cloud SQL, etc.).

### Volúmenes

- **mysql-data:** Base de datos
  - Ubicación: `/var/lib/mysql`
  - Persistente entre reinicios

- **app-logs:** Logs de aplicación
  - Desarrollo: `app-logs`
  - Producción: `app-logs-prod`

### Healthchecks

- **WebApp:** `/health`
  - Intervalo: 30s
  - Timeout: 30s
  - Retries: 3

- **MySQL:**
  - Test: `mysqladmin ping`
  - Intervalo: 10s
  - Timeout: 5s
  - Retries: 5

## Base de Datos

La base de datos utiliza schemas separados para organizar la información:
- `identity`: Usuarios y permisos
- `shop`: Inventario, productos, ventas y operaciones de tienda
- `shared`: Datos compartidos

### Migrations

Las migraciones se manejan usando Entity Framework Core:

#### Crear una Nueva Migración

```bash
# Comando básico
dotnet ef migrations add MigrationName --project src/App.Models.Data --startup-project src/App.Web

# Comando completo con contexto y configuración específica
dotnet ef migrations add MigrationName ^
    --context ApplicationDbContext ^
    --startup-project ./src/App.Web/App.Web.csproj ^
    --project ./src/App.Models.Data ^
    --configuration Release ^
    -- --environment Development
```

#### Aplicar Migraciones

```bash
# Comando básico
dotnet ef database update --project src/App.Models.Data --startup-project src/App.Web

# Comando completo con contexto y configuración específica
dotnet ef database update ^
    --context ApplicationDbContext ^
    --startup-project ./src/App.Web/App.Web.csproj ^
    --project ./src/App.Models.Data ^
    --configuration Release ^
    -- --environment Development
```

> **Notas:**
> - Asegúrate de tener las herramientas de EF Core instaladas: `dotnet tool install --global dotnet-ef`
> - Verifica que las variables de entorno y cadenas de conexión estén configuradas correctamente
> - En Windows, usa `^` en lugar de `\` para dividir comandos largos en múltiples líneas

## Inicio Rápido

### Desarrollo con Docker

1. Clonar repositorio:
```bash
git clone https://github.com/yourusername/Cleeny.git
cd Cleeny
```

2. Configurar variables de entorno:
```bash
cp .env.development.example .env.development
cp .env.development.secrets.example .env.development.secrets
# Editar ambos archivos con tus valores (.secrets nunca se commitea)
```

3. Ejecutar con Docker:
```bash
docker compose --profile development --env-file .\.env.development --env-file .\.env.development.secrets up -d
```

4. Acceder a:
- WebApp: http://localhost:8080

### Desarrollo sin Docker

1. Configurar `appsettings.Development.json`:
```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=Cleeny;User=root;Password=your_password;"
  }
}
```

2. Crear base de datos local:
```sql
CREATE DATABASE Cleeny CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

3. Ejecutar:
```bash
dotnet run --project src/App.Web
```

### Manejo de Zonas Horarias

- Todas las fechas y horas se almacenan en **UTC** en la base de datos
- Los timestamps de auditoría (created_at, modified_at) siempre están en UTC
- La interfaz de usuario muestra las fechas/horas en la zona horaria del cliente
- Los contenedores Docker usan `TZ=UTC` por defecto

## Seguridad

- Autenticación Identity Framework
- Autorización basada en roles
- Auditoría automática
- Soft delete
- Logging comprehensivo
- HTTPS forzado en producción

## Planes Futuros

- Migración a Clean Architecture
- Implementación de CQRS
- Tests unitarios y de integración
- Pipeline de CI/CD

## Contribuir

1. Fork el repositorio
2. Crear rama de feature (`git checkout -b feature/NuevaCaracteristica`)
3. Commit cambios (`git commit -m 'Agrega nueva característica'`)
4. Push a la rama (`git push origin feature/NuevaCaracteristica`)
5. Crear Pull Request

## Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.
