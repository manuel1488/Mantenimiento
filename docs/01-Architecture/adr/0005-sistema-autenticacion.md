### ADR-005: Sistema de Autenticación y Autorización

**Estado:** Actualizado  
**Fecha:** 2024-01-04

**Contexto:** 
El sistema necesita un enfoque simplificado pero efectivo para la autenticación y autorización, aprovechando ASP.NET Core Identity y el sistema de Claims.

**Decisión:** 
Implementar un sistema basado en Identity Framework con las siguientes características:

1. **Modelo Base**:
   - `ApplicationUser`: Extiende IdentityUser con campos adicionales
     - Nombre completo
     - Auditoría
     - Soft delete
   - `ApplicationRoles`: Roles predefinidos en constantes
   - `ApplicationClaims`: Claims predefinidos por módulo

2. **Servicios**:
   - `IIdentityService`: Gestión de usuarios
   - `IRoleService`: Gestión de roles
   - `IdentitySeeder`: Inicialización de datos

3. **Características Principales**:
   - Autenticación basada en nombre de usuario (no email)
   - Email opcional
   - Claims predefinidos por módulo
   - Soft delete para todas las entidades
   - Auditoría automática
   - Soporte multiidioma

4. **Estructura de Claims**:
```csharp
public static class ApplicationClaims
{
    public static class Shop { ... }
    public static class Workshop { ... }
    public static class Admin { ... }
    public static class Shared { ... }
}
```

**Cambios realizados:**
1. Simplificación del modelo de permisos usando Claims directamente
2. Eliminación de tablas adicionales de permisos
3. Uso de constantes para roles y claims
4. Implementación de seeding automático

**Consecuencias:**
- Positivas:
  - Sistema más simple y mantenible
  - Menor complejidad en la base de datos
  - Mejor rendimiento
  - Más fácil de entender y mantener
- Negativas:
  - Menos flexibilidad en permisos personalizados
  - Claims predefinidos requieren recompilación para cambios

**Estructura de Archivos:**
```plaintext
DA.Core/
├── Constants/
│   ├── ApplicationRoles.cs
│   └── ApplicationClaims.cs
├── Identity/
│   └── Interfaces/
│       ├── IIdentityService.cs
│       └── IRoleService.cs

DA.Services/Identity/
├── IdentityService.cs
├── RoleService.cs
└── IdentitySeeder.cs
```

**Referencias:**
- [ASP.NET Core Identity](https://docs.microsoft.com/aspnet/core/security/authentication/identity)
- [Claims-based authorization](https://docs.microsoft.com/aspnet/core/security/authorization/claims)