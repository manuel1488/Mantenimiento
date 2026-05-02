using System.Globalization;

using Microsoft.AspNetCore.DataProtection;

using App.Core.Constants;
using App.Core.Identity.Interfaces;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Interfaces.Identity;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Core.Options;
using App.Core.Services;
using App.Core.Validators;
using App.Models.Data.Contexts;
using App.Models.Data.Interceptors;
using App.Models.Identity;
using App.Services;
using App.Services.Billing;
using App.Services.Customers;
using App.Services.Dashboard;
using App.Services.Data;
using App.Services.Email;
using App.Services.Identity;
using App.Services.Images;
using App.Services.inventory;
using App.Services.Inventory;
using App.Services.Location;
using App.Services.Locations;
using App.Services.Mappings;
using App.Services.Products;
using App.Services.Reports;
using App.Services.Seeders;
using App.Services.Settings;
using App.Services.Shop;
using App.Services.Templates;
using App.Services.Tickets;
using App.Shared.Services;
using App.Shared.Services.Implementation;
using App.Web.Components;
using App.Web.Components.Account;
using App.Web.Services;
using App.Web.Services.Background;
using App.Web.Middleware;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Translations;

using Serilog;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Increase max request header size to prevent HTTP 431 errors from large auth cookies
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 128 * 1024; // 128 KB (default is 32 KB)
});

// Configure EPPlus license context
var licenseContext = builder.Configuration.GetSection("EPPlus:LicenseContext").Value ?? "NonCommercial";
ExcelPackage.LicenseContext = Enum.Parse<LicenseContext>(licenseContext, true);

// Configure Serilog for application logging
ConfigureLogging(builder);

// Configure application options
ConfigureApplicationOptions(builder.Services, builder.Configuration);

// Register core services and configurations
ConfigureServices(builder.Services, builder.Configuration);

// Configure database context and options
ConfigureDatabase(builder.Services, builder.Configuration);

// Configure Identity and Authentication
ConfigureIdentity(builder.Services);

// Configure Localization
ConfigureLocalization(builder.Services);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline
ConfigurePipeline(app);

// Initialize database and seed data
await InitializeDatabase(app);

app.Run();


#region Configuration Methods

/// <summary>
/// Configures application options and culture settings
/// </summary>
void ConfigureApplicationOptions(IServiceCollection services, IConfiguration configuration)
{
    // Configure and validate ApplicationOptions with data annotations
    services.AddOptions<ApplicationOptions>()
        .Bind(configuration.GetSection(ApplicationOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Register CultureOptions as Singleton for managing supported cultures
    services.AddSingleton<CultureOptions>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<ApplicationOptions>>().Value;
        return new CultureOptions(options);
    });

    services.Configure<ExportOptions>(configuration.GetSection(ExportOptions.SectionName));

    builder.Services.Configure<ProductCodeGeneratorOptions>(
        builder.Configuration.GetSection(ProductCodeGeneratorOptions.SectionName));

}

/// <summary>
/// Configures Serilog logging for the application
/// </summary>
void ConfigureLogging(WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    builder.Host.UseSerilog();
}

/// <summary>
/// Configures core services and dependencies
/// </summary>
void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Configure options
    services.Configure<DatabaseOptions>(
        configuration.GetSection(DatabaseOptions.SectionName));

    // Theme configuration
    services.AddSingleton<CurrentThemeService>();

    // MudBlazor configuration
    services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
        config.SnackbarConfiguration.PreventDuplicates = true;
        config.SnackbarConfiguration.NewestOnTop = false;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 5000;
        config.SnackbarConfiguration.HideTransitionDuration = 500;
        config.SnackbarConfiguration.ShowTransitionDuration = 500;

        // Resize options
        config.ResizeOptions.ReportRate = 100;
        config.ResizeOptions.EnableLogging = false;
        config.ResizeOptions.SuppressInitEvent = true;
        config.ResizeOptions.NotifyOnBreakpointOnly = true;
    });

    services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = 5 * 1024 * 1024;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(90); // ✅
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15); // ✅
        options.MaximumParallelInvocationsPerClient = 1;
    });

    services.AddMudTranslations();

    // Register application services
    ConfigureApplicationServices(services, configuration);

    // Configure routing options
    services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    // Configure image service
    services.Configure<ImageOptions>(
        builder.Configuration.GetSection(ImageOptions.SectionName));

    services.Configure<CameraOptions>(
        builder.Configuration.GetSection(CameraOptions.SectionName));

    services.AddControllers();
    services.AddHttpContextAccessor();
    services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment();
            options.DisconnectedCircuitMaxRetained = 100; // ✅
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3); // ✅
            options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1); // ✅
            options.MaxBufferedUnacknowledgedRenderBatches = 10; // ✅
        });
    services.AddRazorPages();

    // Configurar autorización basada en políticas
    services.AddAuthorization(options =>
    {
        // Admin policies
        options.AddPolicy(ApplicationClaims.Admin.ViewUsers, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewUsers));
        options.AddPolicy(ApplicationClaims.Admin.ManageUsers, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageUsers));
        options.AddPolicy(ApplicationClaims.Admin.DeleteUsers, policy => policy.RequireClaim(ApplicationClaims.Admin.DeleteUsers));
        options.AddPolicy(ApplicationClaims.Admin.ResetPassword, policy => policy.RequireClaim(ApplicationClaims.Admin.ResetPassword));

        options.AddPolicy(ApplicationClaims.Admin.ViewRoles, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewRoles));
        options.AddPolicy(ApplicationClaims.Admin.ManageRoles, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageRoles));
        options.AddPolicy(ApplicationClaims.Admin.DeleteRoles, policy => policy.RequireClaim(ApplicationClaims.Admin.DeleteRoles));

        options.AddPolicy(ApplicationClaims.Admin.ViewSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewFiscalSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewFiscalSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageFiscalSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageFiscalSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewBillingSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewBillingSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageBillingSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageBillingSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewGlobalInvoices, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewGlobalInvoices));
        options.AddPolicy(ApplicationClaims.Admin.CreateGlobalInvoice, policy => policy.RequireClaim(ApplicationClaims.Admin.CreateGlobalInvoice));
        options.AddPolicy(ApplicationClaims.Admin.CancelGlobalInvoice, policy => policy.RequireClaim(ApplicationClaims.Admin.CancelGlobalInvoice));

        options.AddPolicy(ApplicationClaims.Admin.ViewWarehouseSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewWarehouseSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageWarehouseSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageWarehouseSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewBranchSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewBranchSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageBranchSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageBranchSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewEmailSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewEmailSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageEmailSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageEmailSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewTaxRates, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewTaxRates));
        options.AddPolicy(ApplicationClaims.Admin.ManageTaxRates, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageTaxRates));
        options.AddPolicy(ApplicationClaims.Admin.DeleteTaxRates, policy => policy.RequireClaim(ApplicationClaims.Admin.DeleteTaxRates));

        options.AddPolicy(ApplicationClaims.Admin.ViewUnitMeasures, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewUnitMeasures));
        options.AddPolicy(ApplicationClaims.Admin.ManageUnitMeasures, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageUnitMeasures));

        options.AddPolicy(ApplicationClaims.Admin.ManageInitialSetup, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageInitialSetup));

        options.AddPolicy(ApplicationClaims.Admin.ViewAudit, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewAudit));
        options.AddPolicy(ApplicationClaims.Admin.ViewAuditReports, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewAuditReports));

        options.AddPolicy(ApplicationClaims.Admin.ViewPermissions, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewPermissions));
        options.AddPolicy(ApplicationClaims.Admin.ManagePermissions, policy => policy.RequireClaim(ApplicationClaims.Admin.ManagePermissions));

        options.AddPolicy(ApplicationClaims.Admin.ViewDisccounts, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewDisccounts));
        options.AddPolicy(ApplicationClaims.Admin.ManageDisccounts, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageDisccounts));

        options.AddPolicy(ApplicationClaims.Admin.ManageTicketSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageTicketSettings));
        options.AddPolicy(ApplicationClaims.Admin.ViewTicketSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewTicketSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewCashiers, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewCashiers));
        options.AddPolicy(ApplicationClaims.Admin.ManageCashiers, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageCashiers));

        options.AddPolicy(ApplicationClaims.Admin.ViewCashStations, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewCashStations));
        options.AddPolicy(ApplicationClaims.Admin.ManageCashStations, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageCashStations));

        // Shop policies
        options.AddPolicy(ApplicationClaims.Shop.ViewInventory, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInventory));
        options.AddPolicy(ApplicationClaims.Shop.ManageInventory, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageInventory));
        options.AddPolicy(ApplicationClaims.Shop.ExportInventory, policy => policy.RequireClaim(ApplicationClaims.Shop.ExportInventory));

        options.AddPolicy(ApplicationClaims.Shop.ViewProducts, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewProducts));
        options.AddPolicy(ApplicationClaims.Shop.ManageProducts, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageProducts));
        options.AddPolicy(ApplicationClaims.Shop.DeleteProducts, policy => policy.RequireClaim(ApplicationClaims.Shop.DeleteProducts));
        options.AddPolicy(ApplicationClaims.Shop.BulkImportProducts, policy => policy.RequireClaim(ApplicationClaims.Shop.BulkImportProducts));

        options.AddPolicy(ApplicationClaims.Shop.ViewPrices, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewPrices));
        options.AddPolicy(ApplicationClaims.Shop.ManagePrices, policy => policy.RequireClaim(ApplicationClaims.Shop.ManagePrices));
        options.AddPolicy(ApplicationClaims.Shop.ManageDiscounts, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageDiscounts));

        options.AddPolicy(ApplicationClaims.Shop.ViewSales, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewSales));
        options.AddPolicy(ApplicationClaims.Shop.ViewHistorySales, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewHistorySales));
        options.AddPolicy(ApplicationClaims.Shop.CreateSale, policy => policy.RequireClaim(ApplicationClaims.Shop.CreateSale));
        options.AddPolicy(ApplicationClaims.Shop.CancelSale, policy => policy.RequireClaim(ApplicationClaims.Shop.CancelSale));
        options.AddPolicy(ApplicationClaims.Shop.ViewSalesReport, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewSalesReport));
        options.AddPolicy(ApplicationClaims.Shop.ViewDailySalesSummary, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewDailySalesSummary));
        options.AddPolicy(ApplicationClaims.Shop.ExportDailySalesSummary, policy => policy.RequireClaim(ApplicationClaims.Shop.ExportDailySalesSummary));

        options.AddPolicy(ApplicationClaims.Shop.CreateInvoice, policy => policy.RequireClaim(ApplicationClaims.Shop.CreateInvoice));
        options.AddPolicy(ApplicationClaims.Shop.ViewInvoice, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInvoice));
        options.AddPolicy(ApplicationClaims.Shop.CancelInvoice, policy => policy.RequireClaim(ApplicationClaims.Shop.CancelInvoice));
        options.AddPolicy(ApplicationClaims.Shop.ViewStampBalance, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewStampBalance));
        options.AddPolicy(ApplicationClaims.Shop.ReceiveStampAlertEmails, policy => policy.RequireClaim(ApplicationClaims.Shop.ReceiveStampAlertEmails));

        options.AddPolicy(ApplicationClaims.Shop.ViewWarehouses, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewWarehouses));
        options.AddPolicy(ApplicationClaims.Shop.ManageWarehouses, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageWarehouses));
        options.AddPolicy(ApplicationClaims.Shop.DeleteWarehouses, policy => policy.RequireClaim(ApplicationClaims.Shop.DeleteWarehouses));

        options.AddPolicy(ApplicationClaims.Shop.ViewCashRegister, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewCashRegister));
        options.AddPolicy(ApplicationClaims.Shop.ManageCashRegister, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageCashRegister));
        options.AddPolicy(ApplicationClaims.Shop.WithdrawCashRegister, policy => policy.RequireClaim(ApplicationClaims.Shop.WithdrawCashRegister));
        options.AddPolicy(ApplicationClaims.Shop.ViewCashRegisterHistory, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewCashRegisterHistory));
        options.AddPolicy(ApplicationClaims.Shop.ViewCashRegisterReport, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewCashRegisterReport));

        options.AddPolicy(ApplicationClaims.Shop.ViewQuotations, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewQuotations));
        options.AddPolicy(ApplicationClaims.Shop.CreateQuotation, policy => policy.RequireClaim(ApplicationClaims.Shop.CreateQuotation));
        options.AddPolicy(ApplicationClaims.Shop.EditQuotation, policy => policy.RequireClaim(ApplicationClaims.Shop.EditQuotation));
        options.AddPolicy(ApplicationClaims.Shop.DeleteQuotation, policy => policy.RequireClaim(ApplicationClaims.Shop.DeleteQuotation));
        options.AddPolicy(ApplicationClaims.Shop.SendQuotation, policy => policy.RequireClaim(ApplicationClaims.Shop.SendQuotation));
        options.AddPolicy(ApplicationClaims.Shop.ConvertQuotationToRemission, policy => policy.RequireClaim(ApplicationClaims.Shop.ConvertQuotationToRemission));
        options.AddPolicy(ApplicationClaims.Shop.ConvertQuotationToSale, policy => policy.RequireClaim(ApplicationClaims.Shop.ConvertQuotationToSale));

        options.AddPolicy(ApplicationClaims.Shop.ViewRemissions, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewRemissions));
        options.AddPolicy(ApplicationClaims.Shop.CreateRemission, policy => policy.RequireClaim(ApplicationClaims.Shop.CreateRemission));
        options.AddPolicy(ApplicationClaims.Shop.CancelRemission, policy => policy.RequireClaim(ApplicationClaims.Shop.CancelRemission));
        options.AddPolicy(ApplicationClaims.Shop.ConsolidateRemissions, policy => policy.RequireClaim(ApplicationClaims.Shop.ConsolidateRemissions));

        options.AddPolicy(ApplicationClaims.Shop.ViewInventoryHistory, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInventoryHistory));
        options.AddPolicy(ApplicationClaims.Shop.ViewInventoryTransfers, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInventoryTransfers));
        options.AddPolicy(ApplicationClaims.Shop.ManageInventoryTransfers, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageInventoryTransfers));
        options.AddPolicy(ApplicationClaims.Shop.ViewInventoryInputs, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInventoryInputs));
        options.AddPolicy(ApplicationClaims.Shop.ManageInventoryInputs, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageInventoryInputs));
        options.AddPolicy(ApplicationClaims.Shop.ViewInventoryAdjustments, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInventoryAdjustments));
        options.AddPolicy(ApplicationClaims.Shop.ManageInventoryAdjustments, policy => policy.RequireClaim(ApplicationClaims.Shop.ManageInventoryAdjustments));
        options.AddPolicy(ApplicationClaims.Shop.ViewInventoryAlerts, policy => policy.RequireClaim(ApplicationClaims.Shop.ViewInventoryAlerts));
        options.AddPolicy(ApplicationClaims.Shop.ReceiveInventoryAlertEmails, policy => policy.RequireClaim(ApplicationClaims.Shop.ReceiveInventoryAlertEmails));

        // Shared policies
        options.AddPolicy(ApplicationClaims.Shared.ViewCustomers, policy => policy.RequireClaim(ApplicationClaims.Shared.ViewCustomers));
        options.AddPolicy(ApplicationClaims.Shared.ManageCustomers, policy => policy.RequireClaim(ApplicationClaims.Shared.ManageCustomers));
        options.AddPolicy(ApplicationClaims.Shared.DeleteCustomers, policy => policy.RequireClaim(ApplicationClaims.Shared.DeleteCustomers));

        options.AddPolicy(ApplicationClaims.Shared.ViewSuppliers, policy => policy.RequireClaim(ApplicationClaims.Shared.ViewSuppliers));
        options.AddPolicy(ApplicationClaims.Shared.ManageSuppliers, policy => policy.RequireClaim(ApplicationClaims.Shared.ManageSuppliers));
        options.AddPolicy(ApplicationClaims.Shared.DeleteSuppliers, policy => policy.RequireClaim(ApplicationClaims.Shared.DeleteSuppliers));

        options.AddPolicy(ApplicationClaims.Shared.ViewDashboard, policy => policy.RequireClaim(ApplicationClaims.Shared.ViewDashboard));
        options.AddPolicy(ApplicationClaims.Shared.ViewReports, policy => policy.RequireClaim(ApplicationClaims.Shared.ViewReports));
        options.AddPolicy(ApplicationClaims.Shared.GenerateReports, policy => policy.RequireClaim(ApplicationClaims.Shared.GenerateReports));

        // Políticas compuestas para verificar acceso a módulos completos
        options.AddPolicy(ApplicationClaims.Admin.AdminAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type.StartsWith("Admin."))));

        options.AddPolicy(ApplicationClaims.Shared.SharedAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type.StartsWith("Shared."))));

        options.AddPolicy(ApplicationClaims.Shop.ShopAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type.StartsWith("Shop."))));

        // Billing (Factura Electrónica) access — user has any invoice or global invoice claim
        options.AddPolicy(ApplicationClaims.Billing.BillingAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c =>
                    c.Type == ApplicationClaims.Shop.ViewInvoice ||
                    c.Type == ApplicationClaims.Shop.ViewStampBalance ||
                    c.Type == ApplicationClaims.Admin.ViewGlobalInvoices)));

        options.AddPolicy(ApplicationClaims.Admin.ViewBillingReports, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewBillingReports));
        options.AddPolicy(ApplicationClaims.Admin.ExportBillingReports, policy => policy.RequireClaim(ApplicationClaims.Admin.ExportBillingReports));

        // Reports section access — any report claim
        options.AddPolicy(ApplicationClaims.Shared.ReportsAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c =>
                    c.Type == ApplicationClaims.Shop.ViewSalesReport ||
                    c.Type == ApplicationClaims.Admin.ViewBillingReports)));

        // Labels policies
        options.AddPolicy(ApplicationClaims.Labels.ViewLabels, policy => policy.RequireClaim(ApplicationClaims.Labels.ViewLabels));
        options.AddPolicy(ApplicationClaims.Labels.PrintLabels, policy => policy.RequireClaim(ApplicationClaims.Labels.PrintLabels));
        options.AddPolicy(ApplicationClaims.Labels.LabelsAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type.StartsWith("Labels."))));
    });

    // Agregar Health Checks
    services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>()
        .AddCheck("self", () => HealthCheckResult.Healthy());

    services.AddRazorTemplating();

    services.Configure<RazorViewEngineOptions>(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Views/Reports/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Reports/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Quotations/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Remissions/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/GlobalInvoices/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/{0}.cshtml");

        options.ViewLocationFormats.Add("/Views/Reports/{1}/_ViewImports.cshtml");
        options.ViewLocationFormats.Add("/Views/Reports/_ViewImports.cshtml");
        options.ViewLocationFormats.Add("/Views/_ViewImports.cshtml");
    });

    services.Configure<FileStorageOptions>(options =>
    {
        options.TempPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "temp");
        Directory.CreateDirectory(options.TempPath);
    });

}

/// <summary>
/// Configures Entity Framework database context
/// </summary>
void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    var databaseOptions = configuration
        .GetSection(DatabaseOptions.SectionName)
        .Get<DatabaseOptions>();

    // Persist Data Protection keys in MySQL so they survive container restarts/redeployments.
    // Without this, cookies and antiforgery tokens are invalidated on every redeploy.
    services.AddDataProtection()
        .PersistKeysToDbContext<ApplicationDbContext>()
        .SetApplicationName("Cleeny");

    // DbContextFactory con configuración independiente
    services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
    {
        // Usamos una configuración que no depende del ServiceProvider
        options.UseMySql(databaseOptions!.ConnectionString,
            ServerVersion.AutoDetect(databaseOptions.ConnectionString),
            mySqlOptions =>
            {
                mySqlOptions.CommandTimeout(databaseOptions.CommandTimeout);
                mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                mySqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            });

        var dateTime = serviceProvider.GetRequiredService<IDateTime>();
        var interceptor = new AuditableEntityInterceptor(dateTime);
        options.AddInterceptors(interceptor);

        if (databaseOptions.EnableDetailedErrors)
            options.EnableDetailedErrors();

        if (databaseOptions.EnableSensitiveDataLogging)
            options.EnableSensitiveDataLogging();
    });
}

/// <summary>
/// Configures Identity and Authentication services
/// </summary>
void ConfigureIdentity(IServiceCollection services)
{
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        // User settings
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
        options.User.RequireUniqueEmail = false;

        // SignIn settings
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

    ConfigureAuthentication(services);
}

/// <summary>
/// Configures authentication and cookie settings
/// </summary>
void ConfigureAuthentication(IServiceCollection services)
{
    services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });

    services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name = "App.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.LoginPath = "/Account/login";
        options.LogoutPath = "/Account/logout";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        ConfigureCookieEvents(options);
    });

    services.AddCascadingAuthenticationState();
    services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
}

/// <summary>
/// Configures application localization settings
/// </summary>
void ConfigureLocalization(IServiceCollection services)
{
    services.AddLocalization(options => options.ResourcesPath = "Resources");

    var applicationSection = builder.Configuration.GetRequiredSection(ApplicationOptions.SectionName);
    var defaultLanguage = applicationSection.GetRequiredSection("DefaultLanguage").Value!;
    var supportedLanguages = applicationSection.GetRequiredSection("SupportedLanguages").Get<string[]>()
    ?? throw new InvalidOperationException("SupportedLanguages configuration is required");

    var defaultCulture = new CultureInfo(defaultLanguage);

    CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
    CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
    CultureInfo.CurrentCulture = defaultCulture;

    services.Configure<RequestLocalizationOptions>(options =>
    {
        options.SetDefaultCulture(defaultLanguage)
           .AddSupportedCultures(supportedLanguages)
           .AddSupportedUICultures(supportedLanguages);

        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new QueryStringRequestCultureProvider(),
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider()
        };
    });
}

/// <summary>
/// Configures the HTTP request pipeline
/// </summary>
void ConfigurePipeline(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseStatusCodePagesWithRedirects("/not-found");

        app.UseHsts();
    }

    var templatesPath = Path.Combine(app.Environment.WebRootPath, "EmailTemplates");
    Directory.CreateDirectory(templatesPath);

    var tempPath = app.Configuration.GetValue<string>("FileStorage:TempPath")
        ?? Path.Combine(app.Environment.ContentRootPath, "Temp");
    var absoluteTempPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, tempPath));
    Directory.CreateDirectory(absoluteTempPath);

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(absoluteTempPath),
        RequestPath = "/temp"
    });
    app.UseSerilogRequestLogging();
    app.UseCookiePolicy();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    app.UseRequestLocalization();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.MapRazorPages();
    app.MapRazorComponents<AppRoot>().AddInteractiveServerRenderMode();
    app.MapAdditionalIdentityEndpoints();
    app.MapControllers();

    // Mapear endpoint de health check
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                Status = report.Status.ToString(),
                Time = DateTime.UtcNow,
                Checks = report.Entries.Select(x => new
                {
                    Component = x.Key,
                    Status = x.Value.Status.ToString(),
                    Description = x.Value.Description
                })
            };
            await context.Response.WriteAsJsonAsync(response);
        }
    });
}

/// <summary>
/// Configures application services
/// </summary>
void ConfigureApplicationServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddMemoryCache();
    services.AddSingleton<IDateTime, DateTimeService>();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddScoped<AuditableEntityInterceptor>();
    services.AddScoped<IIdentitySeeder, IdentitySeeder>();
    services.AddScoped<IdentityRedirectManager>();
    services.AddScoped<ICustomerService, CustomerService>();
    services.AddScoped<ISupplierService, SupplierService>();
    services.AddScoped<IIdentityService, IdentityService>();
    services.AddScoped<IRoleService, RoleService>();
    services.AddScoped<IImageService, ImageService>();
    services.AddScoped<IProductService, ProductService>();
    services.AddScoped<IUnitMeasureService, UnitMeasureService>();
    services.AddScoped<IUnitMeasureSeeder, UnitMeasureSeeder>();
    services.AddScoped<ILocationService, LocationService>();
    services.AddScoped<IUserLocationService, UserLocationService>();
    services.AddScoped<ILocationTicketSettingsService, LocationTicketSettingsService>();
    services.AddScoped<IInventoryQueryService, InventoryQueryService>();
    services.AddScoped<ITemplateService, TemplateService>();
    services.AddScoped<IContextualInventoryService, InventoryService>();
    services.AddScoped<IInventoryService>(sp => sp.GetRequiredService<IContextualInventoryService>());
    services.AddScoped<IBulkLoadResultsExporter, BulkLoadResultsExporter>();
    services.AddScoped<ICompanySettingsService, CompanySettingsService>();
    services.AddScoped<ILookupService, LookupService>();
    services.AddScoped<IGeneralSeeder, GeneralSeeder>();
    services.AddScoped<TaxIdValidator>();
    services.AddScoped<ITaxSettingsService, TaxSettingsService>();
    services.AddScoped<IMexicoFiscalCatalogService, MexicoFiscalCatalogService>();
    services.AddAutoMapper(typeof(UserMappingProfile));

    services.AddScoped<IFiscalCatalogDataReader>(sp =>
    {
        var dataPath = configuration["FiscalCatalogs:DataPath"]
            ?? throw new ArgumentNullException("FiscalCatalogs:DataPath", "FiscalCatalogs:DataPath configuration is required");

        return new CsvFiscalCatalogReader(
            dataPath,
            sp.GetRequiredService<ILogger<CsvFiscalCatalogReader>>()
        );
    });

    services.AddScoped<IMexicoFiscalSeeder, MexicoFiscalCatalogSeeder>();
    services.AddScoped<ICfdiPostalCodeService, CfdiPostalCodeService>();
    services.AddScoped<ICfdiPostalCodeSeeder>(sp =>
    {
        var dataPath = configuration["FiscalCatalogs:DataPath"]
            ?? throw new ArgumentNullException("FiscalCatalogs:DataPath", "FiscalCatalogs:DataPath configuration is required");
        return new CfdiPostalCodeSeeder(
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
            sp.GetRequiredService<ILogger<CfdiPostalCodeSeeder>>(),
            sp.GetRequiredService<IDateTime>(),
            Path.Combine(dataPath, "cfdi_postal_codes.csv")
        );
    });
    services.AddScoped<IEmailSettingsService, EmailSettingsService>();

    services.AddScoped<IEmailSettingsService, EmailSettingsService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IEmailTemplateSettingsService, EmailTemplateSettingsService>();
    services.AddScoped<IEmailTemplateService, EmailTemplateService>();
    services.AddScoped<IEmailTemplateSeeder>(sp => new EmailTemplateSeeder(
        sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
        sp.GetRequiredService<ILogger<EmailTemplateSeeder>>()));
    services.AddScoped<IQuotationSettingsSeeder>(sp => new QuotationSettingsSeeder(
        sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
        sp.GetRequiredService<ILogger<QuotationSettingsSeeder>>()));

    services.AddSingleton<IFileProvider>(new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

    builder.Services.AddScoped<IInventoryHistoryService, InventoryHistoryService>();
    builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
    builder.Services.AddRazorTemplating();
    builder.Services.AddSingleton<IPdfService, PdfService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<ITaxRateService, TaxRateService>();
    services.AddSingleton<InventoryMovementService>();
    builder.Services.AddSingleton<InventoryEventsService>();
    builder.Services.AddScoped<IPermissionCheckService, PermissionCheckService>();
    builder.Services.AddSingleton<PermissionTranslationService>();
    builder.Services.AddSingleton<RoleTranslationService>();
    services.AddScoped<IDiscountSettingsService, DiscountSettingsService>();
    services.AddScoped<IWholesaleSettingsService, WholesaleSettingsService>();
    services.AddScoped<IRoundingSettingsService, RoundingSettingsService>();
    services.AddScoped<ILabelSettingsService, LabelSettingsService>();
    services.AddScoped<IPaymentMethodService, PaymentMethodService>();
    services.AddScoped<IPricingCalculationService, PricingCalculationService>();
    services.AddScoped<IContextualSaleService, SaleService>();
    services.AddScoped<ISaleService>(sp => sp.GetRequiredService<IContextualSaleService>());
    services.AddScoped<IPartialSaleFractionService, PartialSaleFractionService>();
    services.AddScoped<IProductPartialSurchargeService, ProductPartialSurchargeService>();
    services.AddScoped<IWholesaleTierService, WholesaleTierService>();
    services.AddScoped<IProductWholesalePriceService, ProductWholesalePriceService>();
    services.AddScoped<ICustomerSeeder, CustomerSeeder>();
    services.AddScoped<IPaymentMethodSeeder, PaymentMethodSeeder>();
    builder.Services.AddScoped<IDiscountAuthorizerService, DiscountAuthorizerService>();
    builder.Services.AddScoped<ISalesReportService, SalesReportService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddScoped<IThermalPrinterService, ThermalPrinterService>();
    builder.Services.AddScoped<IPasswordValidationService, PasswordValidationService>();
    builder.Services.AddScoped<IProductCodeGeneratorService, ProductCodeGeneratorService>();
    builder.Services.AddScoped<IInventoryAlertEmailService, InventoryAlertEmailService>();
    builder.Services.AddScoped<IStockEntryService, StockEntryService>();
    builder.Services.AddScoped<IAdjustmentEntryService, AdjustmentEntryService>();
    builder.Services.AddScoped<IExcelProcessingService, ExcelProcessingService>();
    builder.Services.AddScoped<IInventoryColumnMappingService, InventoryColumnMappingService>();
    builder.Services.AddScoped<ICashRegisterService, CashRegisterService>();
    services.AddScoped<ICashierProfileService, CashierProfileService>();
    services.AddScoped<ICashStationService, CashStationService>();
    builder.Services.AddScoped<App.Services.Labels.BarcodeGeneratorService>();
    builder.Services.AddScoped<IBulkLabelService, App.Services.Labels.BulkLabelService>();
    builder.Services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();
    builder.Services.AddScoped<IQuotationService, QuotationService>();
    builder.Services.AddScoped<IQuotationSettingsService, QuotationSettingsService>();
    builder.Services.AddScoped<IRemissionService, RemissionService>();

    // Mexico CFDI billing
    services.AddHttpClient();
    services.AddScoped<IMexicoPacSettingsService, MexicoPacSettingsService>();
    services.AddScoped<IMexicoCfdiXmlService, MexicoCfdiXmlService>();
    services.AddScoped<IMexicoCsdSigningService, MexicoCsdSigningService>();
    services.AddScoped<ISwSapienService, SwSapienService>();
    services.AddScoped<IMexicoInvoiceService, MexicoInvoiceService>();
    services.AddScoped<IGlobalInvoiceService, GlobalInvoiceService>();
    services.AddScoped<IInvoiceReportService, InvoiceReportService>();
    services.AddScoped<IMexicoStampAlertService, MexicoStampAlertService>();

    // Cancellation monitor — singleton so UI can inject and call TriggerAsync
    services.AddSingleton<CancellationMonitorService>();
    services.AddHostedService(sp => sp.GetRequiredService<CancellationMonitorService>());
}

/// <summary>
/// Configures cookie authentication events
/// </summary>
void ConfigureCookieEvents(CookieAuthenticationOptions options)
{
    options.Events = new CookieAuthenticationEvents
    {
        OnSigningIn = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Signing in user: {Name}",
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnSignedIn = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogInformation("User signed in: {Name}",
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnValidatePrincipal = async context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Validating principal for: {Name}",
                context.Principal?.Identity?.Name);
            await Task.CompletedTask;
        }
    };
}

/// <summary>
/// Initializes the database and seeds initial data
/// </summary>
async Task InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
    var unitMeasureSeeder = scope.ServiceProvider.GetRequiredService<IUnitMeasureSeeder>();
    var generalSeeder = scope.ServiceProvider.GetRequiredService<IGeneralSeeder>();
    var mexicoFiscalSeeder = scope.ServiceProvider.GetRequiredService<IMexicoFiscalSeeder>();
    var cfdiPostalCodeSeeder = scope.ServiceProvider.GetRequiredService<ICfdiPostalCodeSeeder>();
    var customerSeeder = scope.ServiceProvider.GetRequiredService<ICustomerSeeder>();
    var paymentMethodSeeder = scope.ServiceProvider.GetRequiredService<IPaymentMethodSeeder>();
    var emailTemplateSeeder = scope.ServiceProvider.GetRequiredService<IEmailTemplateSeeder>();
    var quotationSettingsSeeder = scope.ServiceProvider.GetRequiredService<IQuotationSettingsSeeder>();

    await context.Database.MigrateAsync();
    await seeder.SeedAsync();
    await mexicoFiscalSeeder.SeedAsync();
    await cfdiPostalCodeSeeder.SeedAsync();
    await unitMeasureSeeder.SeedAsync();
    await generalSeeder.SeedAsync();
    await customerSeeder.SeedAsync();
    await paymentMethodSeeder.SeedAsync();
    await emailTemplateSeeder.SeedAsync();
    await quotationSettingsSeeder.SeedAsync();
}

#endregion