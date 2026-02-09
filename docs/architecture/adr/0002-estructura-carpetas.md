### ADR-002: Estructura Detallada de Carpetas y Archivos

**Estado:** Actualizado  
**Fecha:** 2024-01-04

**Contexto:**
Necesitamos una estructura de proyecto más simplificada que soporte:
- Múltiples dominios (Shop y Workshop) en una única aplicación
- Separación clara de responsabilidades
- Fácil mantenimiento
- Soporte para Identity personalizado
- Sistema de permisos basado en claims

**Decisión:**
Implementar la siguiente estructura simplificada:

```plaintext
DA.sln
├── src/
│   ├── DA.Core/                    # Interfaces y contratos base
│   │   ├── Interfaces/
│   │   │   ├── IAuditableEntity.cs
│   │   │   └── ISoftDelete.cs
│   │   ├── Constants/
│   │   │   ├── ApplicationRoles.cs
│   │   │   └── ApplicationClaims.cs
│   │   ├── Identity/
│   │   │   └── Interfaces/
│   │   └── Options/
│   ├── DA.Models/                  # Modelos de dominio
│   │   ├── Identity/
│   │   │   └── ApplicationUser.cs
│   │   ├── Shop/
│   │   ├── Workshop/
│   │   └── Shared/
│   ├── DA.Models.Data/            # Acceso a datos
│   │   ├── Contexts/
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   └── Interceptors/
│   ├── DA.Services/               # Lógica de negocio
│   │   ├── Identity/
│   │   │   ├── IdentityService.cs
│   │   │   ├── RoleService.cs
│   │   │   └── IdentitySeeder.cs
│   │   ├── Shop/
│   │   └── Workshop/
│   ├── DA.Shared/                # Utilidades compartidas
│   │   ├── Extensions/
│   │   └── Services/
│   └── DA.Web/                   # Aplicación Blazor
│       ├── Components/
│       │   └── App.razor
│       ├── Pages/
│       │   └── _Host.cshtml
│       └── Services/
└── tests/
    ├── DA.Core.Tests/
    ├── DA.Services.Tests/
    └── DA.Web.Tests/
```

**Cambios realizados:**
1. Simplificación de la estructura de carpetas
2. Eliminación de capas innecesarias
3. Consolidación de servicios relacionados con Identity
4. Mejor organización de los componentes Blazor
5. Estructura más plana y mantenible

**Consecuencias:**
- Positivas:
  - Estructura más simple y directa
  - Menor complejidad en la navegación del código
  - Mejor cohesión entre componentes relacionados
  - Más fácil de mantener y entender
- Negativas:
  - Requiere migración de código existente
  - Necesidad de actualizar referencias