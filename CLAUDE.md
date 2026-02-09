# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
