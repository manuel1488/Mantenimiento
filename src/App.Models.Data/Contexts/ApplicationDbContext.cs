using App.Core.Interfaces;
using App.Models.Billing;
using App.Models.Data.Extensions;
using App.Models.Identity;
using App.Models.Settings;
using App.Models.Shared;
using App.Models.Shop;
using PaymentMethod = App.Models.Settings.PaymentMethod;

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
    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<TaxSettings> TaxSettings { get; set; } = null!;
    public DbSet<TaxRate> TaxRates { get; set; } = null!;
    public DbSet<DiscountSettings> DiscountSettings { get; set; } = null!;
    public DbSet<WholesaleSettings> WholesaleSettings { get; set; } = null!;
    public DbSet<RoundingSettings> RoundingSettings { get; set; } = null!;
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
    public DbSet<CashRegisterSettings> CashRegisterSettings { get; set; } = null!;
    public DbSet<LabelSettings> LabelSettings { get; set; } = null!;
    public DbSet<EmailTemplateSettings> EmailTemplateSettings { get; set; } = null!;
    #endregion

    #region Shared
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerFiscalProfile> CustomerFiscalProfiles { get; set; } = null!;
    #endregion

    #region Shop
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<LocationTicketSettings> LocationTicketSettings { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<UnitMeasure> UnitMeasures { get; set; } = null!;
    public DbSet<Inventory> Inventory { get; set; } = null!;
    public DbSet<InventoryMovement> InventoryMovements { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleDetail> SaleDetails { get; set; } = null!;
    public DbSet<SalePayment> SalePayments { get; set; } = null!;
    public DbSet<TicketConfiguration> TicketConfigurations { get; set; } = null!;
    public DbSet<QuotationSettings> QuotationSettings { get; set; } = null!;
    public DbSet<PartialSaleFraction> PartialSaleFractions { get; set; } = null!;
    public DbSet<ProductPartialSurcharge> ProductPartialSurcharges { get; set; } = null!;
    public DbSet<WholesaleTier> WholesaleTiers { get; set; } = null!;
    public DbSet<ProductWholesalePrice> ProductWholesalePrices { get; set; } = null!;
    public DbSet<CashRegister> CashRegisters { get; set; } = null!;
    public DbSet<CashRegisterMovement> CashRegisterMovements { get; set; } = null!;
    public DbSet<CashRegisterDenomination> CashRegisterDenominations { get; set; } = null!;
    public DbSet<CashStation> CashStations { get; set; } = null!;
    public DbSet<BulkLabelJob> BulkLabelJobs { get; set; } = null!;
    public DbSet<StockEntry> StockEntries { get; set; } = null!;
    public DbSet<StockEntryItem> StockEntryItems { get; set; } = null!;
    public DbSet<AdjustmentEntry> AdjustmentEntries { get; set; } = null!;
    public DbSet<AdjustmentEntryItem> AdjustmentEntryItems { get; set; } = null!;
    public DbSet<Quotation> Quotations { get; set; } = null!;
    public DbSet<QuotationDetail> QuotationDetails { get; set; } = null!;
    public DbSet<Remission> Remissions { get; set; } = null!;
    public DbSet<RemissionDetail> RemissionDetails { get; set; } = null!;
    public DbSet<DocumentSequence> DocumentSequences { get; set; } = null!;
    #endregion

    #region Identity Extensions
    public DbSet<UserLocation> UserLocations { get; set; } = null!;
    public DbSet<CashierProfile> CashierProfiles { get; set; } = null!;
    #endregion

    #region Billing México
    public DbSet<MexicoInvoice> MexicoInvoices { get; set; } = null!;
    public DbSet<MexicoInvoiceFile> MexicoInvoiceFiles { get; set; } = null!;
    public DbSet<GlobalInvoice> GlobalInvoices { get; set; } = null!;
    public DbSet<GlobalInvoiceSale> GlobalInvoiceSales { get; set; } = null!;
    public DbSet<MexicoPacSettings> MexicoPacSettings { get; set; } = null!;
    public DbSet<MexicoFiscalRegime> MexicoFiscalRegimes { get; set; } = null!;
    public DbSet<MexicoPaymentForm> MexicoPaymentForms { get; set; } = null!;
    public DbSet<MexicoPaymentMethod> MexicoPaymentMethods { get; set; } = null!;
    public DbSet<MexicoCfdiUse> MexicoCfdiUses { get; set; } = null!;
    public DbSet<MexicoProductService> MexicoProductServices { get; set; } = null!;
    public DbSet<MexicoSatUnit> MexicoSatUnits { get; set; } = null!;
    public DbSet<MexicoStampAlertSettings> MexicoStampAlertSettings { get; set; } = null!;
    public DbSet<CfdiPostalCode> CfdiPostalCodes { get; set; } = null!;
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
