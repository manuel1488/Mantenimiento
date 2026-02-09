# Estructura del Proyecto

Este documento describe la estructura arquitectónica actualizada del proyecto DA (Detallado Automotriz).

## Diagrama de Arquitectura

```mermaid
graph TB
    subgraph Presentation["Capa de Presentación"]
        WEB["DA.Web (Blazor Server)"]
        WEB --> Components["Components"]
        WEB --> Pages["Pages"]
        Components --> Layouts["Layout"]
        Components --> Shared["Shared"]
    end

    subgraph Core["Core"]
        CoreLayer["DA.Core"]
        CoreLayer --> Constants["Constants"]
        CoreLayer --> Interfaces["Interfaces"]
        CoreLayer --> Identity["Identity/Interfaces"]
        CoreLayer --> Options["Options"]

        Models["DA.Models"]
        Models --> IdentityModels["Identity"]
        Models --> ShopModels["Shop"]
        Models --> WorkshopModels["Workshop"]
        Models --> SharedModels["Shared"]

        Services["DA.Services"]
        Services --> IdentityServices["Identity"]
        Services --> ShopServices["Shop"]
        Services --> WorkshopServices["Workshop"]

        Shared["DA.Shared"]
        Shared --> Extensions["Extensions"]
        Shared --> SharedServices["Services"]
    end

    subgraph Data["Capa de Datos"]
        ModelsData["DA.Models.Data"]
        ModelsData --> Context["Contexts"]
        ModelsData --> Configurations["Configurations"]
        ModelsData --> Interceptors["Interceptors"]
    end

    subgraph Tests["Tests"]
        CoreTests["DA.Core.Tests"]
        ServicesTests["DA.Services.Tests"]
        WebTests["DA.Web.Tests"]
    end

    WEB --> Services
    WEB --> Models
    Services --> Models
    Services --> CoreLayer
    ModelsData --> Models
    ModelsData --> CoreLayer
```

## Descripción de Capas

### Presentación (DA.Web)
- **Propósito**: Interfaz de usuario usando Blazor Server con MudBlazor
- **Componentes Principales**:
  - `Components/`: Componentes reutilizables
  - `Pages/`: Páginas principales
  - `Services/`: Servicios específicos de UI

### Core
#### DA.Core
- **Propósito**: Definiciones base e interfaces
- **Componentes**:
  ```plaintext
  DA.Core/
  ├── Constants/
  │   ├── ApplicationRoles.cs
  │   └── ApplicationClaims.cs
  ├── Interfaces/
  │   ├── IAuditableEntity.cs
  │   └── ISoftDelete.cs
  ├── Identity/
  │   └── Interfaces/
  └── Options/
  ```

#### DA.Models
- **Propósito**: Modelos de dominio
- **Componentes**:
  ```plaintext
  DA.Models/
  ├── Identity/
  │   └── ApplicationUser.cs
  ├── Shop/
  ├── Workshop/
  └── Shared/
  ```

#### DA.Services
- **Propósito**: Lógica de negocio
- **Componentes**:
  ```plaintext
  DA.Services/
  ├── Identity/
  │   ├── IdentityService.cs
  │   ├── RoleService.cs
  │   └── IdentitySeeder.cs
  ├── Shop/
  └── Workshop/
  ```

#### DA.Shared
- **Propósito**: Utilidades y servicios compartidos
- **Componentes**:
  ```plaintext
  DA.Shared/
  ├── Extensions/
  └── Services/
      ├── IDateTime.cs
      └── ICurrentUserService.cs
  ```

### Datos (DA.Models.Data)
- **Propósito**: Acceso a datos y configuración de EF Core
- **Componentes**:
  ```plaintext
  DA.Models.Data/
  ├── Contexts/
  │   └── ApplicationDbContext.cs
  ├── Configurations/
  └── Interceptors/
      └── AuditableEntityInterceptor.cs
  ```

### Tests
- **Propósito**: Pruebas unitarias e integración
- **Proyectos**:
  - `DA.Core.Tests/`
  - `DA.Services.Tests/`
  - `DA.Web.Tests/`

## Características Principales

### Identity
- Autenticación basada en username
- Claims predefinidos por módulo
- Roles del sistema
- Seeding automático

### Auditoría
- Campos automáticos:
  - CreatedBy/CreatedAt
  - ModifiedBy/ModifiedAt
  - DeletedBy/DeletedAt
- Soft delete en todas las entidades

### Internacionalización
- Soporte multi-idioma (i18n)
- Recursos localizados
- Español como idioma predeterminado

## Referencias
- [ASP.NET Core Blazor](https://docs.microsoft.com/aspnet/core/blazor)
- [MudBlazor](https://mudblazor.com)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)