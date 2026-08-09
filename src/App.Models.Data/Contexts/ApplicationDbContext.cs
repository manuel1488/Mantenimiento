using App.Core.Interfaces;
using App.Models.Data.Extensions;
using App.Models.Identity;
using App.Models.Settings;
using App.Models.Shared;

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
    public DbSet<LocalizationSettings> LocalizationSettings { get; set; } = null!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = null!;
    public DbSet<EmailTemplateSettings> EmailTemplateSettings { get; set; } = null!;
    #endregion

    #region Shared
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
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
