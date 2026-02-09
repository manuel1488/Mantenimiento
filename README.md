# DA (Detallado Automotriz)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-purple)](https://docs.microsoft.com/ef/core)
[![MySql](https://img.shields.io/badge/MySQL-8.0-00758F)](https://www.mysql.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Docker](https://img.shields.io/badge/Docker-20.10%2B-2496ED)](https://www.docker.com/)

Un sistema integrado para la gestión de taller mecánico y tienda de repuestos, construido con .NET 8.

## 📋 Estado del Proyecto

Este proyecto representa la fase inicial de implementación con una arquitectura N-Layer. La estructura actual sirve como base para una futura migración hacia Clean Architecture.

## 🌟 Características Planificadas

- **Gestión de Tienda** (`/tienda`)
  - Control de inventario
  - Gestión de productos
  - Ventas y facturación
  - Facturación electrónica

- **Gestión de Taller** (`/taller`)
  - Órdenes de trabajo
  - Seguimiento de vehículos
  - Gestión de servicios
  - Registro fotográfico

- **Características Base**
  - Sistema de autenticación y autorización
  - Auditoría y soft delete
  - Gestión de archivos
  - Configuración flexible
  - Multi-idioma (i18n)
  - Temas personalizables

## 🏗️ Estructura del Proyecto

```
DA/
├── src/
│   ├── DA.Core/              # Contratos base e interfaces
│   ├── DA.Models/            # Entidades y modelos
│   ├── DA.Models.Data/       # Acceso a datos y EF Core
│   ├── DA.Services/          # Lógica de negocio
│   ├── DA.Shared/           # Utilidades compartidas
│   └── DA.Web/              # Aplicación Blazor
├── docker/
│   ├── .dockerfile          # Dockerfile principal
│   └── mysql/
│       └── init/           # Scripts de inicialización
├── .dockerignore           # Archivos ignorados por Docker
├── .env.development.example # Template variables desarrollo
├── .env.production.example  # Template variables producción
└── docs/
    ├── architecture/       # Documentación de arquitectura
    └── guides/            # Guías de usuario y desarrollo
```

## 🔧 Tecnologías

- **.NET 8**
  - ASP.NET Core 8
  - Entity Framework Core 8
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

## 🐳 Docker

### Prerrequisitos
- Docker Desktop 20.10 o superior
- Docker Compose v2
- .NET SDK 8.0 (para desarrollo local)

### Archivos de Configuración

#### .dockerignore
Este archivo especifica qué archivos y directorios deben ser ignorados al construir la imagen Docker:
```text
# .dockerignore
.git
.env*
.vs
.vscode
*.md
**/bin/
**/obj/
**/node_modules/
Dockerfile*
docker-compose*
```

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

#### .env.development.example
```env
# Ambiente
ASPNETCORE_ENVIRONMENT=Development
TZ=America/Mexico_City

# Puertos
PORT=8080
MYSQL_PORT=3306

# Prefijos de contenedores
CONTAINER_PREFIX=da-dev

# MySQL
MYSQL_ROOT_PASSWORD=change_this_password
MYSQL_DATABASE=DA
MYSQL_USER=da_user
MYSQL_PASSWORD=change_this_password

# Recursos
CPU_LIMIT=0.5
MEMORY_LIMIT=512M
CPU_RESERVATION=0.25
MEMORY_RESERVATION=256M

# Conexión
DATABASE_CONNECTION_STRING=Server=db;Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};
```

#### .env.production.example
```env
# Ambiente
ASPNETCORE_ENVIRONMENT=Production
TZ=America/Mexico_City

# Puertos
PORT=8080

# Prefijos de contenedores
CONTAINER_PREFIX=da-prod

# Recursos
CPU_LIMIT=1
MEMORY_LIMIT=1G
CPU_RESERVATION=0.5
MEMORY_RESERVATION=512M

# Conexión (ajustar con valores reales de producción)
DATABASE_CONNECTION_STRING=Server=your_production_host;Database=DA;User=your_production_user;Password=your_production_password;
```

⚠️ **Importante:**
- Nunca comitear los archivos `.env.development` y `.env.production` al repositorio
- Siempre mantener actualizados los archivos `.example` con la estructura correcta
- Cambiar todas las contraseñas por defecto
- En producción, usar valores seguros y únicos para todas las variables

### Estructura Docker
```
DA/
├── .dockerfile                # Dockerfile principal
├── docker-compose.yml        # Configuración de servicios
├── .env.development         # Variables de entorno desarrollo
├── .env.production         # Variables de entorno producción
└── mysql/
    └── init/              # Scripts de inicialización
        └── 01-init.sql   # Script inicial
```

### Inicialización de MySQL

El archivo `mysql/init/01-init.sql` se ejecuta automáticamente cuando el contenedor de MySQL se inicia por primera vez. Este script:

1. Crea la base de datos y configura el charset:
```sql
CREATE DATABASE IF NOT EXISTS `DA`;
USE `DA`;
ALTER DATABASE `DA` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. Crea el usuario de la aplicación y asigna permisos:
```sql
CREATE USER IF NOT EXISTS 'da_user'@'%' IDENTIFIED BY 'da_password';
GRANT ALL PRIVILEGES ON DA.* TO 'da_user'@'%';
FLUSH PRIVILEGES;
```

**Notas importantes:**
- El script solo se ejecuta en la primera inicialización del volumen
- Si se elimina el volumen (`docker compose down -v`), el script se ejecutará nuevamente
- Las credenciales en el script deben coincidir con las variables de entorno
- Para múltiples scripts, se ejecutan en orden alfabético (01-, 02-, etc.)

### Ambientes y Comandos

#### 🔧 Desarrollo

```bash
# Construir imágenes
docker compose --profile development --env-file .\.env.development build --no-cache

# Iniciar servicios
docker compose --profile development --env-file .\.env.development up -d

# Ver logs
docker compose --profile development logs -f

# Detener servicios
docker compose --profile development --env-file .\.env.development down

# Limpiar volúmenes (⚠️ borra datos)
docker compose --profile development --env-file .\.env.development down -v
```

#### 🚀 Producción

```bash
# Construir imágenes
docker compose --profile production --env-file .\.env.production build --no-cache

# Iniciar servicios
docker compose --profile production --env-file .\.env.production up -d

# Detener servicios
docker compose --profile production --env-file .\.env.production down
```

### 📊 Servicios y Puertos

| Servicio | Desarrollo | Producción |
|----------|------------|------------|
| WebApp   | 8080       | 8080       |
| MySQL*   | 3306       | N/A        |

> *MySQL solo se incluye para desarrollo y pruebas. En producción, se recomienda usar un servicio de base de datos administrado (como AWS RDS, Azure Database for MySQL, Google Cloud SQL, etc.).

### 💾 Volúmenes

- **mysql-data:** Base de datos
  - Ubicación: `/var/lib/mysql`
  - Persistente entre reinicios
  - ⚠️ Contiene datos inicializados por 01-init.sql
  
- **app-logs:** Logs de aplicación
  - Desarrollo: `app-logs`
  - Producción: `app-logs-prod`

### 🏥 Healthchecks

- **WebApp:** `/health`
  - Intervalo: 30s
  - Timeout: 30s
  - Retries: 3

- **MySQL:**
  - Test: `mysqladmin ping`
  - Intervalo: 10s
  - Timeout: 5s
  - Retries: 5

## 📦 Base de Datos

La base de datos utiliza schemas separados para organizar la información:
- `identity`: Usuarios y permisos
- `shop`: Datos de la tienda
- `workshop`: Datos del taller
- `shared`: Datos compartidos

### Migrations

Las migraciones se manejan usando Entity Framework Core. A continuación se detallan los comandos principales:

#### Crear una Nueva Migración

```bash
# Comando básico
dotnet ef migrations add MigrationName --project src/DA.Models.Data --startup-project src/DA.Web

# Comando completo con contexto y configuración específica
dotnet ef migrations add Products \
    --context ApplicationDbContext \
    --startup-project ./src/DA.Web/DA.Web.csproj \
    --project ./src/DA.Models.Data \
    --configuration Release \
    -- --environment Development
```

#### Aplicar Migraciones

```bash
# Comando básico
dotnet ef database update --project src/DA.Models.Data --startup-project src/DA.Web

# Comando completo con contexto y configuración específica
dotnet ef database update \
    --context ApplicationDbContext \
    --startup-project ./src/DA.Web/DA.Web.csproj \
    --project ./src/DA.Models.Data \
    --configuration Release \
    -- --environment Development
```

#### Parámetros Importantes

- `--context`: Especifica el contexto de base de datos a utilizar
- `--startup-project`: Proyecto que contiene la configuración de inicio
- `--project`: Proyecto que contiene las migraciones
- `--configuration`: Configuración de compilación (Debug/Release)
- `--environment`: Ambiente de ejecución (Development/Production)

⚠️ **Notas:**
- Asegúrate de tener las herramientas de EF Core instaladas: `dotnet tool install --global dotnet-ef`
- Verifica que las variables de entorno y cadenas de conexión estén configuradas correctamente
- En Windows, usa `^` en lugar de `\` para dividir comandos largos en múltiples líneas

## 🚀 Inicio Rápido

### 🔨 Desarrollo con Docker

1. Clonar repositorio:
```bash
git clone https://github.com/yourusername/DA.git
cd DA
```

2. Configurar variables de entorno:
```bash
cp .env.development.example .env.development
# Editar .env.development con tus valores
```

3. Ejecutar con Docker:
```bash
docker compose --profile development --env-file .\.env.development up -d
```

4. Acceder a:
- WebApp: http://localhost:8080
- Swagger: http://localhost:8080/swagger

### Inicialización de MySQL

⚠️ **Importante para Producción:**
El servicio de MySQL incluido en Docker está pensado ÚNICAMENTE para desarrollo y pruebas. Para producción, se recomienda:
- Usar un servicio de base de datos administrado
- Mantener la base de datos en un ambiente independiente y seguro
- Configurar backups automáticos
- Implementar alta disponibilidad
- Seguir las mejores prácticas de seguridad para bases de datos en producción

Ejemplos de servicios recomendados:
- AWS RDS for MySQL
- Azure Database for MySQL
- Google Cloud SQL
- Digital Ocean Managed Databases

### 📝 Desarrollo sin Docker

1. Configurar `appsettings.Development.json`:
```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=DA;User=root;Password=your_password;"
  }
}
```

2. Crear base de datos local:
```sql
CREATE DATABASE DA CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

3. Ejecutar:
```bash
dotnet run --project src/DA.Web
```

### ⏰ Manejo de Zonas Horarias

El sistema maneja las zonas horarias de la siguiente manera:

#### Almacenamiento
- Todas las fechas y horas se almacenan en UTC en la base de datos
- Los timestamps de auditoría (created_at, modified_at) siempre están en UTC
- Entity Framework está configurado para manejar automáticamente la conversión a UTC

#### Configuración del Servidor
- Los contenedores Docker usan `TZ=UTC` por defecto
- La zona horaria del servidor no afecta el almacenamiento de datos
- Los logs del sistema se registran en UTC para consistencia

#### Presentación al Usuario
- La interfaz de usuario muestra las fechas/horas en la zona horaria del cliente
- La conversión se maneja en el frontend usando JavaScript
- El componente `MudBlazor.DatePicker` está configurado para manejar zonas horarias
- Los formatos de fecha/hora respetan la configuración regional (i18n) del usuario

#### Ejemplo de Flujo
1. Usuario ingresa: "2024-01-19 15:00" (Hora Ciudad de México)
2. Aplicación convierte a UTC antes de almacenar: "2024-01-19 21:00"
3. Base de datos almacena: "2024-01-19 21:00 UTC"
4. Al recuperar:
   - Usuario en México ve: "15:00"
   - Usuario en España ve: "22:00"
   - Usuario en Japón ve: "06:00" (día siguiente)

#### Consideraciones para Desarrollo

```csharp
// En DA.Shared/Services/DateTimeService.cs
public class DateTime
// En DA.Shared/Services/DateTimeService.cs
public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.UtcNow;
}
```

## 🛡️ Seguridad

- Autenticación Identity Framework
- Autorización basada en roles
- Auditoría automática
- Soft delete
- Logging comprehensivo
- HTTPS forzado en producción

## 📚 Documentación Adicional

- [Decisiones de Arquitectura](docs/architecture/adr/)
- [Guías de Desarrollo](docs/guides/)
- [Diagramas](docs/architecture/diagrams/)

## 🔄 Planes Futuros

- Migración a Clean Architecture
- Implementación de CQRS
- Separación de dominios (Shop/Workshop)
- Tests unitarios y de integración
- Pipeline de CI/CD

## 🤝 Contribuir

1. Fork el repositorio
2. Crear rama de feature (`git checkout -b feature/NuevaCaracteristica`)
3. Commit cambios (`git commit -m 'Agrega nueva característica'`)
4. Push a la rama (`git push origin feature/NuevaCaracteristica`)
5. Crear Pull Request

## 🌐 Notas sobre Zonas Horarias y Globalización

- El sistema está diseñado para ser desplegado en cualquier región
- Todas las operaciones internas usan UTC
- La presentación se adapta a la zona horaria y cultura del usuario
- Los reportes indican claramente la zona horaria utilizada

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.