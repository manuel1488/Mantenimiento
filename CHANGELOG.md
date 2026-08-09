# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

## [0.1.0] - 2026-08-09

### Added

- Plantilla base extraída de un proyecto de negocio existente: Identity, roles/claims
  granulares, soft delete, auditoría (`AuditLogInterceptor` + `[SensitiveData]`),
  branding white-label vía `Branding/{profile}.json`, envío y gestión de plantillas de
  email, generación genérica de PDF (HTML/Razor View → PDF), manejo de archivos/imágenes,
  patrón `Result<T>`, `DbContext` factory pattern.
- Migración inicial de EF Core (`InitialCreate`) regenerada desde cero sobre el esquema
  reducido: Identity, `DataProtectionKeys`, `AuditLogs`, `CompanySettings`,
  `LocalizationSettings`, `EmailSettings`, `EmailTemplateSettings`.
