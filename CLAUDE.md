# CLAUDE.md

Quick reference for Claude Code. Detailed documentation is in [`docs/`](docs/).

## Documentation Index

| Folder | Contents |
|---|---|
| [`docs/01-Architecture/`](docs/01-Architecture/) | ADRs, system diagrams, architecture decisions |
| [`docs/02-Development/`](docs/02-Development/) | Development guides and patterns |
| [`docs/03-Modules/`](docs/03-Modules/) | Module-specific documentation |
| [`docs/99-Archive/`](docs/99-Archive/) | Archived / legacy files |

### Key guides
- [MudBlazor DataGrid Guide](docs/02-Development/mudblazor-datagrid-guide.md) — MudDataGrid vs MudTable, column sizing, ServerData
- [Workflow Diagram Guide](docs/02-Development/workflow-diagram-guide.md) — Status flow dialogs with inline SVG (Quotations reference impl.)
- [White-Label Deployment Guide](docs/02-Development/white-label-deployment-guide.md) — Desplegar la app para otra tienda: perfil de marca (`Branding/{tienda}.json`), logo/nombre en BD, checklist de onboarding
- [Troubleshooting Index](docs/02-Development/troubleshooting.md) — Problemas conocidos con diagnóstico y solución
- [Bitácora de Incidentes de Producción](docs/02-Development/incident-log.md) — Registro de incidentes de despliegue/producción con diagnóstico y reparación aplicada
- [Deuda Técnica](docs/02-Development/tech-debt.md) — Problemas conocidos no urgentes (constraints ignorados, columnas mal dimensionadas, etc.)

### Strategy
- [Roadmap SaaS POS](docs/01-Architecture/roadmap-saas-pos.md) — Evolución multi-tenant: agente hardware, terminal PWA tablet, offline, báscula
- [Análisis de Mercado POS México 2026](docs/01-Architecture/market-analysis-pos-mexico-2026.md) — Competidores, brechas, oportunidades de diferenciación

### Related Projects
- **CleenyAgent** (`C:\repos\CleenyAgent`) — Agente local de hardware (impresora + caja). Integración en `src/App.Web/Services/ThermalPrinterService.cs`. API en `http://localhost:9100`, token en `appsettings.json → AgentSettings:Token`.

### ADRs
Índice completo: [docs/01-Architecture/README.md](docs/01-Architecture/README.md)
- [ADR-007: Factura Global](docs/01-Architecture/adr/0007-factura-global-publica-en-general.md) — Períodos fijos SAT, selección individual rechazada, regla 2.7.1.24 RMF
- [ADR-008: AutoMapper](docs/01-Architecture/adr/0008-dependencia-automapper.md) — Downgrade a v12.0.1 MIT, vulnerabilidad GHSA-rvv3-g6hj-g44x (riesgo bajo), migración pendiente a Mapperly
- [ADR-009: Fechas PDF CFDI y regeneración](docs/01-Architecture/adr/0009-fechas-pdf-cfdi-y-regeneracion.md) — FECHA DE EMISIÓN debe usar RequestedInvoiceDate (no StampDate), conversión de zona horaria, flag AllowPdfRegenerationForStampedInvoices

### CFDI References (docs/03-Modules/)
- [Guía de llenado CFDI global v4.0](docs/03-Modules/Guia_llenado_CFDI_global.md) — Nodo InformacionGlobal, concepto único, RFC genérico
- [Anexo 20 — Guía de llenado CFDI](docs/03-Modules/Anexo_20_Guia_de_llenado_CFDI.md) — Especificación técnica CFDI 4.0
- [Reglas de negocio: fechas de CFDI](docs/03-Modules/business-rules-cfdi-fechas.md) — RequestedInvoiceDate vs StampDate vs Fecha del XML, conversión de zona horaria, reglas de regeneración de PDF

## Project Overview
App is a .NET 9 Blazor Server application for product sales and inventory management. The application uses an N-Layer architecture with MySQL database and Entity Framework Core. It includes Mexico CFDI billing support.

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build App.sln

# Run the web application (development)
dotnet run --project src/App.Web

# Build specific configuration
dotnet build src/App.Web --configuration Release
dotnet build src/App.Web --configuration Debug

# Clean build artifacts
dotnet clean src/App.Web

# Publish for release
dotnet publish src/App.Web --configuration Release
```

### Database Management
```bash
# Create new migration
dotnet ef migrations add MigrationName --project src/App.Models.Data --startup-project src/App.Web

# Apply migrations
dotnet ef database update --project src/App.Models.Data --startup-project src/App.Web

# Advanced migration commands with context
dotnet ef migrations add MigrationName \
    --context ApplicationDbContext \
    --startup-project ./src/App.Web/App.Web.csproj \
    --project ./src/App.Models.Data \
    --configuration Release \
    -- --environment Development

dotnet ef database update \
    --context ApplicationDbContext \
    --startup-project ./src/App.Web/App.Web.csproj \
    --project ./src/App.Models.Data \
    --configuration Release \
    -- --environment Development
```

### Docker Development
```bash
# Development environment
docker compose --profile development --env-file .env.development up -d
docker compose --profile development --env-file .env.development build --no-cache
docker compose --profile development --env-file .env.development down

# Production environment
docker compose --profile production --env-file .env.production up -d
docker compose --profile production --env-file .env.production build --no-cache
docker compose --profile production --env-file .env.production down
```

## Architecture Overview

### Project Structure
- **App.Core**: Interfaces, DTOs, enums, and core contracts
- **App.Models**: Entity models and domain objects
- **App.Models.Data**: EF Core configurations, DbContext, and data access
- **App.Services**: Business logic and service implementations
- **App.Shared**: Shared utilities and common functionality
- **App.Web**: Blazor Server UI application

### Key Technologies
- **.NET 9** with ASP.NET Core and Blazor Server
- **Entity Framework Core 9** with MySQL (Pomelo provider)
- **MudBlazor** for UI components with Material Design
- **AutoMapper** for object mapping between layers
- **ASP.NET Identity** for authentication and authorization
- **Serilog** for structured logging
- **Docker** for containerization

### Database Schema Organization
The database uses logical separation with schemas:
- `identity`: User management and authentication
- `shop`: Inventory, products, sales, and store operations
- `shared`: Common data shared across domains

### Service Layer Patterns
- Services use AutoMapper for DTO/Entity mapping
- Mapping profiles are organized by domain in `App.Services/Mappings/`
- Interface segregation principle with specific service interfaces
- Repository pattern implemented through EF Core DbContext

### Key Features
- **Multi-language support** (English/Spanish) with resource files
- **Multi-tenant ready** architecture with proper separation
- **Audit trails** with created/modified timestamps
- **Soft delete** implementation across entities
- **File management** system for document storage
- **Health checks** for application monitoring
- **Mexico CFDI billing** support

### Development Environment
- Uses .NET 9 SDK (configured in global.json)
- MySQL 8 database with UTF8MB4 charset
- Development server runs on port 8080 by default
- Hot reload enabled for development

### Important Notes
- No test projects currently exist in the solution
- Uses conventional commit messages and git workflow
- Environment-specific configuration through appsettings.json files
- Docker setup includes MySQL for development only (external DB recommended for production)
- All DateTime handling uses UTC internally with timezone conversion in UI

### Common Development Patterns
- Entity configurations use Fluent API in `App.Models.Data/Configurations/`
- Controllers follow REST conventions with proper HTTP verbs
- Blazor components organized by feature domains
- Resource files (.resx) for internationalization
- Dependency injection configured in Program.cs

## Development Standards

### Code Language Requirements
**All code must be written in English** - variables, methods, classes, properties, enums, comments, and technical documentation. Only user-facing text should use IStringLocalizer for internationalization.

```csharp
// Correct - English names
private string customerName;
public async Task<Result<Customer>> GetCustomerByIdAsync(int id)

// Incorrect - Spanish names
private string nombreCliente;
public async Task<Result<Customer>> ObtenerClientePorIdAsync(int id)
```

### Result Pattern Usage
**CRITICAL: Always use the Result pattern** from `App.Core.Common.Result` for all service methods. Never throw exceptions for business logic errors - use Result.Failure() instead.

```csharp
// CORRECT - Service method returning value with Result pattern
public async Task<Result<CustomerDto>> GetByIdAsync(int id)
{
    try
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return Result<CustomerDto>.Failure(_localizer["Customer not found"]);

        var dto = _mapper.Map<CustomerDto>(customer);
        return Result<CustomerDto>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving customer {Id}", id);
        return Result<CustomerDto>.Failure(_localizer["Error retrieving customer"]);
    }
}

// CORRECT - Service method returning void with Result pattern
public async Task<Result> DeleteAsync(int id)
{
    try
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return Result.Failure(_localizer["Customer not found"]);

        _context.Customers.Remove(customer);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting customer {Id}", id);
        return Result.Failure(_localizer["Error deleting customer"]);
    }
}

// CORRECT - Consuming the Result pattern in components
var result = await _customerService.GetByIdAsync(id);
if (result.IsSuccess)
{
    var customer = result.Value;
    _snackbar.Add(L["Success"], Severity.Success);
}
else
{
    _snackbar.Add(result.Error, Severity.Error);
}
```

**Key Points:**
- Use `Result<T>` for methods that return data
- Use `Result` (without generic) for methods that return void/success-only
- Always wrap service methods in try-catch blocks
- Return `Result.Failure()` for business logic errors (validation, not found, etc.)
- Catch exceptions only for logging and return generic error messages
- Use `_localizer` for all error messages to support internationalization

### Internationalization with IStringLocalizer
**All user-facing text** must use IStringLocalizer for multi-language support:

```razor
@inject IStringLocalizer<ComponentName> L

<MudButton>@L["Save"]</MudButton>
<MudTextField Label="@L["Customer Name"]"
              RequiredError="@L["Name is required"]" />
```

**Resource file organization**:
- `App.Web/Resources/Components/[Area]/[ComponentName].en.resx`
- `App.Web/Resources/Components/[Area]/[ComponentName].es.resx`

## Development Patterns

### Service Layer Pattern
Follow this structure for all service implementations:

```csharp
public class CustomerService : ICustomerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;
    private readonly IStringLocalizer<CustomerService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public CustomerService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CustomerService> logger,
        IStringLocalizer<CustomerService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = _mapper.Map<Customer>(dto);

            // Set audit fields
            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;
            entity.CreatedBy = currentUser;
            entity.CreatedAt = currentTime;
            entity.ModifiedBy = currentUser;
            entity.ModifiedAt = currentTime;

            context.Customers.Add(entity);
            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<CustomerDto>(entity);
            return Result<CustomerDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return Result<CustomerDto>.Failure(_localizer["Error creating customer"]);
        }
    }
}
```

**Important Service Layer Conventions:**
- Always use `IDbContextFactory<ApplicationDbContext>` with `await using var context`
- Inject `IStringLocalizer<T>` for localized error messages
- Inject `ICurrentUserService` for audit trail tracking
- Inject `IDateTime` for testable time operations
- Set audit fields (CreatedBy, CreatedAt, ModifiedBy, ModifiedAt) on all entities
- Use Result pattern for all return types
- Log errors with structured logging before returning Result.Failure()

### Tax Calculation Pattern
**NEVER hardcode tax rates.** Always use `ITaxRateService` to retrieve configured tax rates from `stg_tax_rates` table.

**Key Points:**
- Inject `ITaxRateService` and `ICompanySettingsService` in all services that calculate taxes
- Use `GetEffectiveRateAsync(countryCode)` to get the current tax rate
- Tax rates are configurable per country/region in the Settings module
- This applies to Sales and any financial calculations

### Blazor Component Pattern
Standard structure for Blazor components:

```razor
@using MudBlazor
@inject IStringLocalizer<ComponentName> L

<!-- Component markup -->

@code {
    #region Parameters
    [Parameter]
    public CustomerDto Customer { get; set; } = new();

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }
    #endregion

    #region Fields
    private MudForm _form = null!;
    private bool _processing = false;
    #endregion

    #region Methods
    private async Task HandleSubmit()
    {
        var result = await _service.CreateAsync(Customer);
        if (result.IsSuccess)
        {
            _snackbar.Add(L["Created successfully"], Severity.Success);
            MudDialog?.Close(DialogResult.Ok(result.Value));
        }
        else
        {
            _snackbar.Add(result.Error, Severity.Error);
        }
    }
    #endregion
}
```

## Common Issues and Solutions

### Material Icons Not Displaying
**Problem**: Icons appear as empty spaces causing layout misalignment.
**Solution**:
1. Ensure Material Icons font is loaded in `Components/AppRoot.razor`:
   ```html
   <link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet" />
   ```
2. Use correct syntax for icons in Razor markup:
   ```razor
   <!-- Incorrect -->
   <MudButton StartIcon="Icons.Material.Filled.Add">

   <!-- Correct -->
   <MudButton StartIcon="@Icons.Material.Filled.Add">
   ```

### MudBlazor Component Issues
- **MudChip**: Always specify type parameter `T="string"` for proper functionality
- **Dialog backdrop**: Use `BackdropClick="false"` to prevent accidental closes (not `DisableBackdropClick`)
- **AlignItems**: Use enum `AlignItems="AlignItems.Center"` instead of string `"Center"`
- **IMudDialogInstance**: Always use interface `IMudDialogInstance` not concrete class `MudDialogInstance`

### Common Fixes
- Replace `DisableBackdropClick = true` with `BackdropClick = false`
- Replace `AlignItems="Center"` with `AlignItems="AlignItems.Center"`
- Add `T="string"` parameter to MudChip components
- Use `@` prefix for all icon references in Razor markup

## Figma Design System Integration Rules

### Technology Stack
- **.NET 9** Blazor Server (Interactive Server rendering)
- **MudBlazor 8.15.0** — primary UI component library (Material Design 3)
- **C# / Razor** — component language
- **Google Fonts** — Roboto typeface + Material Icons (CDN)
- **Bootstrap** — available but minimal use; prefer MudBlazor grid

### Design Tokens

#### Color Palette

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `Primary` | `#E53935` | `#E53935` | Primary actions, CTA, headers |
| `Secondary` | `#757575` | `#757575` | Secondary text, subtle UI |
| `Background` | `#F5F5F5` | `#121212` | Page background |
| `Surface` | `#FFFFFF` | `#1E1E1E` | Cards, paper, drawer, AppBar |
| `Success` | `#4CAF50` | `#4CAF50` | Confirmations, active states |
| `Warning` | `#FF9800` | `#FF9800` | Cautions, alerts |
| `Error` | `#E53935` | `#E53935` | Error states (same as Primary) |
| `Info` | `#2196F3` | `#2196F3` | Informational messages |

Custom CSS variables (from `wwwroot/css/admin.css`):
```css
--da-primary:    #E53935;
--da-secondary:  #757575;
--da-surface:    #FFFFFF;
--da-background: #F5F5F5;
```

#### Typography

| Style | Size | Weight | Line Height |
|-------|------|--------|-------------|
| H1 | 24px | 400 | 1.167 |
| H2 | 20px | 300 | 1.2 |
| Body1 | 14px | 400 | 1.5 |
| Caption | 12px | 400 | 1.66 |
| Default | 14px | 400 | 1.43 |

- **Font Family**: `Roboto, sans-serif`
- **Letter Spacing**: `.01071em` (default)

#### Spacing
MudBlazor uses a **4px base unit**. Utility classes: `pa-0` → `pa-4` (0–16px in steps of 4px).

#### Border Radius

| Class | Value | Usage |
|-------|-------|-------|
| `.da-card` | `8px` | Cards, paper components |
| `.da-button-rounded` | `20px` | Pill-style action buttons |

### Responsive Breakpoints

| Breakpoint | Width | Use Case |
|-----------|-------|----------|
| `xs` | < 600px | Mobile phones — full width |
| `sm` | 600–960px | Tablets — half width |
| `md` | 960–1280px | Small desktop — drawer collapse threshold |
| `lg` | 1280–1920px | Desktop |
| `xl` | > 1920px | Large/ultra-wide desktop |

**Drawer behavior**: Collapses to temporary overlay below `Breakpoint.Md` (960px).

### Icon System

**Source**: Google Material Icons loaded via CDN in `Components/AppRoot.razor`.

```html
<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet" />
```

**Always use the `@` prefix in Razor markup:**
```razor
<!-- ✅ Correct -->
<MudButton StartIcon="@Icons.Material.Filled.Add" />
<MudIconButton Icon="@Icons.Material.Filled.Edit" />

<!-- ❌ Wrong — missing @ prefix -->
<MudButton StartIcon="Icons.Material.Filled.Add" />
```

**Common icons used in this codebase:**
```
Icons.Material.Filled.Dashboard             → Dashboard
Icons.Material.Filled.Store                 → Shop/Sales
Icons.Material.Filled.People                → Customers
Icons.Material.Filled.AdminPanelSettings    → Admin
Icons.Material.Filled.Add                   → Create
Icons.Material.Filled.Edit                  → Edit
Icons.Material.Filled.Delete                → Delete
Icons.Material.Filled.Search                → Search
Icons.Material.Filled.Download              → Export
Icons.Material.Filled.Upload                → Import
Icons.Material.Filled.Print                 → Print
Icons.Material.Filled.Refresh               → Reload
Icons.Material.Filled.Warning               → Destructive confirm
Icons.Material.Filled.CheckCircle           → Success confirm
```

### Figma Component → MudBlazor Mapping

| Figma Component | MudBlazor Component | Notes |
|-----------------|--------------------|----|
| Button (filled) | `<MudButton Variant="Variant.Filled" Color="Color.Primary">` | |
| Button (outlined) | `<MudButton Variant="Variant.Outlined">` | |
| Icon button | `<MudIconButton Icon="@Icons.Material.Filled.X" />` | |
| Text field | `<MudTextField Variant="Variant.Outlined">` | Use Outlined for forms |
| Select / Dropdown | `<MudSelect>` or `<MudAutocomplete>` | |
| Card / Surface | `<MudPaper Elevation="2" Class="pa-4">` | |
| Data table | `<MudDataGrid T="Type">` | Use `SortMode`, not `Sortable` |
| Dialog / Modal | `<MudDialog>` + `IDialogService` | |
| Snackbar / Toast | `ISnackbar.Add(msg, Severity.X)` | Bottom-right, 5s duration |
| Navigation drawer | `<MudDrawer>` | Responsive, clips at `Breakpoint.Md` |
| Chips / Tags | `<MudChip T="string">` | Always specify `T="string"` |
| Toggle | `<MudSwitch>` | |
| Progress / Spinner | `<MudProgressCircular Indeterminate="true">` | |
| Alert / Banner | `<MudAlert Severity="Severity.Warning">` | |
| Avatar / Image | `<MudAvatar>` / `<MudImage>` | |
| Divider | `<MudDivider>` | |
| Tooltip | `<MudTooltip Text="...">` | Use lowercase `title=""` for native HTML |

### Grid System

**12-column responsive grid:**
```razor
<MudGrid>
    <MudItem xs="12" sm="6" md="4" lg="3">
        <!-- xs: full width on mobile -->
        <!-- sm: 1/2 width on tablets -->
        <!-- md: 1/3 width on small desktop -->
        <!-- lg: 1/4 width on desktop -->
    </MudItem>
</MudGrid>
```

**Container max width:** `MaxWidth.ExtraLarge` on all page-level layouts.

### Asset Management

| Asset Type | Location | Format | Notes |
|-----------|---------|--------|-------|
| Application logo | `wwwroot/images/logo.webp` | WebP | 7.9 KB |
| Product images | `wwwroot/uploads/` | JPEG/PNG/WebP | Max 5 MB, 75% JPEG quality |
| Thumbnails | `wwwroot/uploads/thumb_*` | JPEG | Max 300×300 px |
| Favicon | `wwwroot/favicon.ico` / `.png` | ICO/PNG | |
| Email templates | `wwwroot/EmailTemplates/` | HTML | Runtime generated |

### Styling Approach

1. **MudBlazor theme** (`Services/CurrentThemeService.cs`) — single source of truth for colors and typography.
2. **Scoped CSS** (`.razor.css` files) — component-local overrides.
3. **Global utility CSS** (`wwwroot/app.css`) — Blazor/form validation overrides only.
4. **Admin utilities** (`wwwroot/css/admin.css`) — custom classes `.da-card`, `.da-button-rounded`.
5. **No CSS Modules, Tailwind, or SCSS** — plain CSS only.

**Attribute naming critical rule:**
```razor
<!-- ✅ HTML attributes = lowercase -->
<MudIconButton title="Edit" tabindex="0" />

<!-- ✅ Blazor/MudBlazor component properties = PascalCase -->
<MudIconButton Color="Color.Primary" OnClick="@HandleClick" />
```

### Standard Page Component Template (from Figma screen)

```razor
@page "/area/feature"
@using App.Core.DTOs.Domain
@using App.Core.Interfaces
@using MudBlazor

@inject IService Service
@inject ISnackbar Snackbar
@inject IStringLocalizer<FeaturePage> L
@inject IDialogService DialogService

@attribute [Authorize(Policy = ApplicationClaims.Area.Permission)]

<PageTitle>@L["Page Title"]</PageTitle>

<MudText Typo="Typo.h4" GutterBottom="true">@L["Heading"]</MudText>

<MudGrid>
    <MudItem xs="12">
        <MudPaper Elevation="2" Class="pa-4">
            @if (_loading)
            {
                <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
            }
            else
            {
                <!-- Content -->
            }
        </MudPaper>
    </MudItem>
</MudGrid>

@code {
    #region Fields
    private bool _loading = false;
    private List<ItemDto> _items = [];
    #endregion

    #region Lifecycle
    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }
    #endregion

    #region Methods
    private async Task LoadData()
    {
        _loading = true;
        var result = await Service.GetAllAsync();
        if (result.IsSuccess)
            _items = result.Value;
        else
            Snackbar.Add(result.Error, Severity.Error);
        _loading = false;
    }
    #endregion
}
```

### MudBlazor v8 API — Known Gotchas

| Wrong | Correct | Reason |
|-------|---------|--------|
| `Sortable="true"` | `SortMode="SortMode.Multiple"` | Property renamed in v8 |
| `Pageable="true"` | *(removed)* — automatic | Not needed in v8 |
| `PageSize="10"` | `RowsPerPage="10"` | Property renamed in v8 |
| `DisableBackdropClick="true"` | `BackdropClick="false"` | API change |
| `AlignItems="Center"` | `AlignItems="AlignItems.Center"` | Enum, not string |
| `<MudChip>` | `<MudChip T="string">` | Generic type required |
| `MudDialogInstance` | `IMudDialogInstance` | Use interface, not class |
| `Title="..."` on HTML attrs | `title="..."` | MUD0002 warning |

> **MudDataGrid column sizing guide**: See [`docs/02-Development/mudblazor-datagrid-guide.md`](docs/02-Development/mudblazor-datagrid-guide.md). **Always use MudDataGrid over MudTable for data listing pages.**

> **Workflow diagrams**: Use inline SVG inside MudDialog — NOT Z.Blazor.Diagrams (SVG layer fails in dialog portals). See [`docs/02-Development/workflow-diagram-guide.md`](docs/02-Development/workflow-diagram-guide.md).
