using App.Core.Interfaces;
using App.Models.Clientes;
using App.Models.Cotizaciones;
using App.Models.Data.Extensions;
using App.Models.Facturas;
using App.Models.Fiscal;
using App.Models.Identity;
using App.Models.Obras;
using App.Models.Servicios;
using App.Models.Settings;
using App.Models.Shared;
using App.Models.Subcontratistas;
using App.Models.Tecnicos;

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Models.Data.Contexts;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    #region Infrastructure
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    #endregion

    #region Settings
    public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
    public DbSet<MinioConfiguracion> MinioConfiguraciones { get; set; } = null!;
    public DbSet<LocalizationSettings> LocalizationSettings { get; set; } = null!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = null!;
    public DbSet<EmailTemplateSettings> EmailTemplateSettings { get; set; } = null!;
    public DbSet<CotizacionTemplateSettings> CotizacionTemplateSettings { get; set; } = null!;
    #endregion

    #region Shared
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    #endregion

    #region Mantenimiento
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Servicio> Servicios { get; set; } = null!;
    public DbSet<Tecnico> Tecnicos { get; set; } = null!;
    public DbSet<Subcontratista> Subcontratistas { get; set; } = null!;
    public DbSet<Obra> Obras { get; set; } = null!;
    public DbSet<Actividad> Actividades { get; set; } = null!;
    public DbSet<ActividadEvidenciaFoto> ActividadEvidenciaFotos { get; set; } = null!;
    public DbSet<ActividadReasignacion> ActividadReasignaciones { get; set; } = null!;
    public DbSet<Cotizacion> Cotizaciones { get; set; } = null!;
    public DbSet<CotizacionLinea> CotizacionLineas { get; set; } = null!;
    public DbSet<CotizacionFoto> CotizacionFotos { get; set; } = null!;
    public DbSet<Factura> Facturas { get; set; } = null!;
    #endregion

    #region Fiscal Catalogs
    public DbSet<RegimenFiscalCatalogo> RegimenesFiscalesCatalogo { get; set; } = null!;
    public DbSet<UsoCfdiCatalogo> UsosCfdiCatalogo { get; set; } = null!;
    public DbSet<ClaveUnidadSatCatalogo> ClavesUnidadSatCatalogo { get; set; } = null!;
    public DbSet<ClaveProdServSatCatalogo> ClavesProdServSatCatalogo { get; set; } = null!;
    public DbSet<TipoProdServSatCatalogo> TiposProdServSatCatalogo { get; set; } = null!;
    public DbSet<SegmentoProdServSatCatalogo> SegmentosProdServSatCatalogo { get; set; } = null!;
    public DbSet<FamiliaProdServSatCatalogo> FamiliasProdServSatCatalogo { get; set; } = null!;
    #endregion

    #region Servicios Catalogs
    public DbSet<UnidadMedida> UnidadesMedida { get; set; } = null!;
    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply global filters
        builder.ApplyGlobalFilters<ISoftDelete>(e => e.IsDeleted == 0);
    }
}
