using App.Core.Interfaces;
using App.Models.Billing;
using App.Models.Data.Extensions;
using App.Models.Identity;
using App.Models.Settings;
using App.Models.Shared;
using App.Models.Shop;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Models.Data.Contexts;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    #region Settings
    public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
    public DbSet<LocalizationSettings> LocalizationSettings { get; set; } = null!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = null!;
    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<TaxSettings> TaxSettings { get; set; } = null!;
    public DbSet<TaxRate> TaxRates { get; set; } = null!;
    public DbSet<DiscountSettings> DiscountSettings { get; set; } = null!;
    public DbSet<RoundingSettings> RoundingSettings { get; set; } = null!;
    #endregion

    #region Shared
    public DbSet<Customer> Customers { get; set; } = null!;
    #endregion

    #region Shop
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<UnitMeasure> UnitMeasures { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Inventory> Inventory { get; set; } = null!;
    public DbSet<InventoryMovement> InventoryMovements { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleDetail> SaleDetails { get; set; } = null!;
    public DbSet<TicketConfiguration> TicketConfigurations { get; set; } = null!;
    public DbSet<PartialSaleFraction> PartialSaleFractions { get; set; } = null!;
    public DbSet<ProductPartialSurcharge> ProductPartialSurcharges { get; set; } = null!;
    public DbSet<WholesaleTier> WholesaleTiers { get; set; } = null!;
    public DbSet<ProductWholesalePrice> ProductWholesalePrices { get; set; } = null!;
    #endregion

    #region Billing México
    public DbSet<MexicoInvoice> MexicoInvoices { get; set; } = null!;
    public DbSet<MexicoInvoiceFile> MexicoInvoiceFiles { get; set; } = null!;
    public DbSet<MexicoPacSettings> MexicoPacSettings { get; set; } = null!;
    public DbSet<MexicoFiscalRegime> MexicoFiscalRegimes { get; set; } = null!;
    public DbSet<MexicoPaymentForm> MexicoPaymentForms { get; set; } = null!;
    public DbSet<MexicoPaymentMethod> MexicoPaymentMethods { get; set; } = null!;
    public DbSet<MexicoCfdiUse> MexicoCfdiUses { get; set; } = null!;
    public DbSet<MexicoProductService> MexicoProductServices { get; set; } = null!;
    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply global filters
        builder.ApplyGlobalFilters<ISoftDelete>(e =>e.IsDeleted == 0);

        // Configure enum conversions globally
        ConfigureEnumConversions(builder);
    }

    private static void ConfigureEnumConversions(ModelBuilder builder)
    {
        builder.Entity<Sale>()
            .Property(e => e.Status)
            .HasConversion<int>();
    }
}
