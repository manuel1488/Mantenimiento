using System.Globalization;

using App.Core.Constants;
using App.Core.Identity.Interfaces;
using App.Core.Interfaces;
using App.Core.Interfaces.Identity;
using App.Core.Options;
using App.Core.Services;
using App.Models.Data.Contexts;
using App.Models.Data.Interceptors;
using App.Models.Identity;
using App.Services.Email;
using App.Services.Identity;
using App.Services.Images;
using App.Services.Mappings;
using App.Services.Reports;
using App.Services.Seeders;
using App.Services.Settings;
using App.Services.Shared;
using App.Shared.Services;
using App.Shared.Services.Implementation;
using App.Web.Components;
using App.Web.Components.Account;
using App.Web.Middleware;
using App.Web.Services;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Translations;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load the per-tenant brand identity (app name, logo, theme colors) from Branding/{profile}.json.
// Select the profile with the BRANDING_PROFILE env var per deployment — defaults to "default".
// This file is loaded after appsettings.json/appsettings.{Environment}.json so it can override
// their Application:Name, but before environment variables are re-applied by AddEnvironmentVariables
// below, so infra env vars (if ever needed) still win over the committed brand profile.
var brandingProfile = builder.Configuration["BRANDING_PROFILE"] ?? "default";
builder.Configuration.AddJsonFile(
    Path.Combine("Branding", $"{brandingProfile}.json"),
    optional: false,
    reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// Increase max request header size to prevent HTTP 431 errors from large auth cookies
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 128 * 1024; // 128 KB (default is 32 KB)
});

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

    // Configure and validate BrandingOptions (app name is covered by ApplicationOptions above;
    // this only covers visual identity: logo + theme colors) so white-label deployments only need
    // to change appsettings/env vars, not code.
    services.AddOptions<BrandingOptions>()
        .Bind(configuration.GetSection(BrandingOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Register CultureOptions as Singleton for managing supported cultures
    services.AddSingleton<CultureOptions>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<ApplicationOptions>>().Value;
        return new CultureOptions(options);
    });
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
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(90);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
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

    services.AddControllers();
    services.AddHttpContextAccessor();
    services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment();
            options.DisconnectedCircuitMaxRetained = 100;
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
            options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
            options.MaxBufferedUnacknowledgedRenderBatches = 10;
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

        options.AddPolicy(ApplicationClaims.Admin.ViewEmailSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewEmailSettings));
        options.AddPolicy(ApplicationClaims.Admin.ManageEmailSettings, policy => policy.RequireClaim(ApplicationClaims.Admin.ManageEmailSettings));

        options.AddPolicy(ApplicationClaims.Admin.ViewAudit, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewAudit));

        options.AddPolicy(ApplicationClaims.Admin.ViewPermissions, policy => policy.RequireClaim(ApplicationClaims.Admin.ViewPermissions));
        options.AddPolicy(ApplicationClaims.Admin.ManagePermissions, policy => policy.RequireClaim(ApplicationClaims.Admin.ManagePermissions));

        // Shared policies
        options.AddPolicy(ApplicationClaims.Shared.ViewDashboard, policy => policy.RequireClaim(ApplicationClaims.Shared.ViewDashboard));

        // Políticas compuestas para verificar acceso a módulos completos
        options.AddPolicy(ApplicationClaims.Admin.AdminAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type.StartsWith("Admin."))));

        options.AddPolicy(ApplicationClaims.Shared.SharedAccess, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type.StartsWith("Shared."))));
    });

    // Agregar Health Checks
    services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>()
        .AddCheck("self", () => HealthCheckResult.Healthy());

    services.AddRazorTemplating();

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
        .SetApplicationName("AppBase");

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
                mySqlOptions.EnableRetryOnFailure(databaseOptions.MaxRetryCount);
            });

        var dateTime = serviceProvider.GetRequiredService<IDateTime>();
        // Order matters: the auditable interceptor must run first so soft-deletes are
        // converted (Deleted -> Modified + IsDeleted bump) before the audit log classifies them.
        var auditableInterceptor = new AuditableEntityInterceptor(dateTime);
        var auditLogInterceptor = new AuditLogInterceptor(dateTime);
        options.AddInterceptors(auditableInterceptor, auditLogInterceptor);

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
    services.AddScoped<IIdentityService, IdentityService>();
    services.AddScoped<IRoleService, RoleService>();
    services.AddScoped<IImageService, ImageService>();
    services.AddScoped<ICompanySettingsService, CompanySettingsService>();
    services.AddAutoMapper(typeof(UserMappingProfile));

    services.AddScoped<IEmailSettingsService, EmailSettingsService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IEmailTemplateSettingsService, EmailTemplateSettingsService>();
    services.AddScoped<IEmailTemplateService, EmailTemplateService>();
    services.AddScoped<IEmailTemplateSeeder>(sp => new EmailTemplateSeeder(
        sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
        sp.GetRequiredService<ILogger<EmailTemplateSeeder>>()));
    services.AddScoped<ICompanyBrandingSeeder, CompanyBrandingSeeder>();

    services.AddSingleton<IFileProvider>(new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

    services.AddSingleton<IPdfService, PdfService>();
    services.AddScoped<IPermissionCheckService, PermissionCheckService>();
    services.AddSingleton<PermissionTranslationService>();
    services.AddSingleton<RoleTranslationService>();
    services.AddScoped<IPasswordValidationService, PasswordValidationService>();
    services.AddScoped<IAuditLogService, AuditLogService>();
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
    var emailTemplateSeeder = scope.ServiceProvider.GetRequiredService<IEmailTemplateSeeder>();
    var companyBrandingSeeder = scope.ServiceProvider.GetRequiredService<ICompanyBrandingSeeder>();

    await context.Database.MigrateAsync();
    await seeder.SeedAsync();
    await emailTemplateSeeder.SeedAsync();
    await companyBrandingSeeder.SeedAsync();
}

#endregion
