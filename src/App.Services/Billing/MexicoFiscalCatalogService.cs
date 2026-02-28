using AutoMapper;

using App.Core.DTOs.Billing;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Interfaces;
using App.Models.Billing;
using App.Models.Data.Contexts;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Billing;

public class MexicoFiscalCatalogService : IMexicoFiscalCatalogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<MexicoFiscalCatalogService> _logger;
    private readonly IStringLocalizer<MexicoFiscalCatalogService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public MexicoFiscalCatalogService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<MexicoFiscalCatalogService> logger,
        IStringLocalizer<MexicoFiscalCatalogService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    #region Generic Helper Methods

    private async Task<T?> GetByIdAsync<T>(int id) where T : class
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        return await _context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    private async Task<T?> GetByCodeAsync<T>(string code) where T : class
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        return await _context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<string>(e, "Code") == code);
    }

    private async Task<TDto> CreateAsync<T, TDto, TCreateDto>(TCreateDto createDto)
        where T : class
        where TDto : class
        where TCreateDto : CreateMexicoFiscalCatalogDto
    {
        // Validate unique code
        if (await ValidateUniqueCodeAsync<T>(createDto.Code))
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var entity = _mapper.Map<T>(createDto);
            
            // Set audit fields
            typeof(T).GetProperty("CreatedBy")?.SetValue(entity, _currentUserService.FullName ?? "Unknown");
            typeof(T).GetProperty("CreatedAt")?.SetValue(entity, _dateTime.Now);

            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<TDto>(entity);
        }

        throw new InvalidOperationException(_localizer["Code already exists"]);
    }

    private async Task<TDto> UpdateAsync<T, TDto>(int id, UpdateMexicoFiscalCatalogDto updateDto)
        where T : class
        where TDto : class
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        var entity = await _context.Set<T>()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);

        if (entity == null)
        {
            throw new InvalidOperationException(_localizer["Record not found"]);
        }

        // Update properties
        _mapper.Map(updateDto, entity);

        // Update audit fields
        typeof(T).GetProperty("ModifiedBy")?.SetValue(entity, _currentUserService.FullName ?? "Unknown");
        typeof(T).GetProperty("ModifiedAt")?.SetValue(entity, _dateTime.Now);

        await _context.SaveChangesAsync();

        return _mapper.Map<TDto>(entity);
    }
    #endregion

    #region Fiscal Regimes
    public async Task<IList<MexicoFiscalRegimeDto>> GetFiscalRegimesAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var query = _context.Set<MexicoFiscalRegime>().AsNoTracking();

            var items = await query.OrderBy(x => x.Code)
                .Select(x => _mapper.Map<MexicoFiscalRegimeDto>(x))
                .ToListAsync();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fiscal regimes");
            throw;
        }
    }

    public async Task<MexicoFiscalRegimeDto?> GetFiscalRegimeByIdAsync(int id)
    {
        try
        {
            var entity = await GetByIdAsync<MexicoFiscalRegime>(id);
            return entity != null ? _mapper.Map<MexicoFiscalRegimeDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fiscal regime by id {Id}", id);
            throw;
        }
    }

    public async Task<MexicoFiscalRegimeDto?> GetFiscalRegimeByCodeAsync(string code)
    {
        try
        {
            var entity = await GetByCodeAsync<MexicoFiscalRegime>(code);
            return entity != null ? _mapper.Map<MexicoFiscalRegimeDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fiscal regime by code {Code}", code);
            throw;
        }
    }

    async Task<MexicoFiscalRegimeDto> IMexicoFiscalCatalogService.CreateFiscalRegimeAsync(CreateMexicoFiscalRegimeDto createDto)
    {
        try
        {
            return await CreateAsync<MexicoFiscalRegime, MexicoFiscalRegimeDto, CreateMexicoFiscalRegimeDto>(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fiscal regime");
            throw;
        }
    }

    async Task<MexicoFiscalRegimeDto> IMexicoFiscalCatalogService.UpdateFiscalRegimeAsync(int id, UpdateMexicoFiscalCatalogDto updateDto)
    {
        try
        {
            return await UpdateAsync<MexicoFiscalRegime, MexicoFiscalRegimeDto>(id, updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fiscal regime {Id}", id);
            throw;
        }
    }
    #endregion

    #region Payment Forms
    public async Task<IList<MexicoPaymentFormDto>> GetPaymentFormsAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var query = _context.Set<MexicoPaymentForm>().AsNoTracking();

            var items = await query.OrderBy(x => x.Code)
                .Select(x => _mapper.Map<MexicoPaymentFormDto>(x))
                .ToListAsync();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment forms");
            throw;
        }
    }

    public async Task<MexicoPaymentFormDto?> GetPaymentFormByIdAsync(int id)
    {
        try
        {
            var entity = await GetByIdAsync<MexicoPaymentForm>(id);
            return entity != null ? _mapper.Map<MexicoPaymentFormDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment form by id {Id}", id);
            throw;
        }
    }

    public async Task<MexicoPaymentFormDto?> GetPaymentFormByCodeAsync(string code)
    {
        try
        {
            var entity = await GetByCodeAsync<MexicoPaymentForm>(code);
            return entity != null ? _mapper.Map<MexicoPaymentFormDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment form by code {Code}", code);
            throw;
        }
    }

    async Task<MexicoPaymentFormDto> IMexicoFiscalCatalogService.CreatePaymentFormAsync(CreateMexicoPaymentFormDto createDto)
    {
        try
        {
            return await CreateAsync<MexicoPaymentForm, MexicoPaymentFormDto, CreateMexicoPaymentFormDto>(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment form");
            throw;
        }
    }

    async Task<MexicoPaymentFormDto> IMexicoFiscalCatalogService.UpdatePaymentFormAsync(int id, UpdateMexicoFiscalCatalogDto updateDto)
    {
        try
        {
            return await UpdateAsync<MexicoPaymentForm, MexicoPaymentFormDto>(id, updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment form {Id}", id);
            throw;
        }
    }
    #endregion

    #region Payment Methods
    public async Task<IList<MexicoPaymentMethodDto>> GetPaymentMethodsAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var query = _context.Set<MexicoPaymentMethod>().AsNoTracking();

            var items = await query.OrderBy(x => x.Code)
                .Select(x => _mapper.Map<MexicoPaymentMethodDto>(x))
                .ToListAsync();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment methods");
            throw;
        }
    }

    public async Task<MexicoPaymentMethodDto?> GetPaymentMethodByIdAsync(int id)
    {
        try
        {
            var entity = await GetByIdAsync<MexicoPaymentMethod>(id);
            return entity != null ? _mapper.Map<MexicoPaymentMethodDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment method by id {Id}", id);
            throw;
        }
    }

    public async Task<MexicoPaymentMethodDto?> GetPaymentMethodByCodeAsync(string code)
    {
        try
        {
            var entity = await GetByCodeAsync<MexicoPaymentMethod>(code);
            return entity != null ? _mapper.Map<MexicoPaymentMethodDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment method by code {Code}", code);
            throw;
        }
    }

    async Task<MexicoPaymentMethodDto> IMexicoFiscalCatalogService.CreatePaymentMethodAsync(CreateMexicoPaymentMethodDto createDto)
    {
        try
        {
            return await CreateAsync<MexicoPaymentMethod, MexicoPaymentMethodDto, CreateMexicoPaymentMethodDto>(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            throw;
        }
    }

    async Task<MexicoPaymentMethodDto> IMexicoFiscalCatalogService.UpdatePaymentMethodAsync(int id, UpdateMexicoFiscalCatalogDto updateDto)
    {
        try
        {
            return await UpdateAsync<MexicoPaymentMethod, MexicoPaymentMethodDto>(id, updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method {Id}", id);
            throw;
        }
    }
    #endregion

    #region CFDI Uses
    public async Task<IList<MexicoCfdiUseDto>> GetCfdiUsesAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var query = _context.Set<MexicoCfdiUse>().AsNoTracking();

            var items = await query.OrderBy(x => x.Code)
                .Select(x => _mapper.Map<MexicoCfdiUseDto>(x))
                .ToListAsync();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CFDI uses");
            throw;
        }
    }

    public async Task<IList<MexicoCfdiUseDto>> GetCfdiUsesByFiscalRegimeAsync(string fiscalRegimeCode)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var items = await _context.Set<MexicoCfdiUse>()
                .AsNoTracking()
                .Where(x => x.FiscalRegimeCodes == null || x.FiscalRegimeCodes.Contains(fiscalRegimeCode))
                .OrderBy(x => x.Code)
                .ToListAsync();

            return items.Select(x => _mapper.Map<MexicoCfdiUseDto>(x)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CFDI uses for fiscal regime {FiscalRegimeCode}", fiscalRegimeCode);
            throw;
        }
    }

    public async Task<MexicoCfdiUseDto?> GetCfdiUseByIdAsync(int id)
    {
        try
        {
            var entity = await GetByIdAsync<MexicoCfdiUse>(id);
            return entity != null ? _mapper.Map<MexicoCfdiUseDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CFDI use by id {Id}", id);
            throw;
        }
    }

    public async Task<MexicoCfdiUseDto?> GetCfdiUseByCodeAsync(string code)
    {
        try
        {
            var entity = await GetByCodeAsync<MexicoCfdiUse>(code);
            return entity != null ? _mapper.Map<MexicoCfdiUseDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CFDI use by code {Code}", code);
            throw;
        }
    }

    async Task<MexicoCfdiUseDto> IMexicoFiscalCatalogService.CreateCfdiUseAsync(CreateMexicoCfdiUseDto createDto)
    {
        try
        {
            return await CreateAsync<MexicoCfdiUse, MexicoCfdiUseDto, CreateMexicoCfdiUseDto>(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating CFDI use");
            throw;
        }
    }

    async Task<MexicoCfdiUseDto> IMexicoFiscalCatalogService.UpdateCfdiUseAsync(int id, UpdateMexicoFiscalCatalogDto updateDto)
    {
        try
        {
            return await UpdateAsync<MexicoCfdiUse, MexicoCfdiUseDto>(id, updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CFDI use {Id}", id);
            throw;
        }
    }
    #endregion

    #region Product Services
    public async Task<IList<MexicoProductServiceDto>> GetProductServicesAsync()
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var query = _context.Set<MexicoProductService>().AsNoTracking();

            var items = await query.OrderBy(x => x.Code)
                .Select(x => _mapper.Map<MexicoProductServiceDto>(x))
                .ToListAsync();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product services");
            throw;
        }
    }

    public async Task<MexicoProductServiceDto?> GetProductServiceByIdAsync(int id)
    {
        try
        {
            var entity = await GetByIdAsync<MexicoProductService>(id);
            return entity != null ? _mapper.Map<MexicoProductServiceDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product service by id {Id}", id);
            throw;
        }
    }

    public async Task<MexicoProductServiceDto?> GetProductServiceByCodeAsync(string code)
    {
        try
        {
            var entity = await GetByCodeAsync<MexicoProductService>(code);
            return entity != null ? _mapper.Map<MexicoProductServiceDto>(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product service by code {Code}", code);
            throw;
        }
    }

    async Task<MexicoProductServiceDto> IMexicoFiscalCatalogService.CreateProductServiceAsync(CreateMexicoProductServiceDto createDto)
    {
        try
        {
            return await CreateAsync<MexicoProductService, MexicoProductServiceDto, CreateMexicoProductServiceDto>(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product service");
            throw;
        }
    }

    async Task<MexicoProductServiceDto> IMexicoFiscalCatalogService.UpdateProductServiceAsync(int id, UpdateMexicoFiscalCatalogDto updateDto)
    {
        try
        {
            return await UpdateAsync<MexicoProductService, MexicoProductServiceDto>(id, updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product service {Id}", id);
            throw;
        }
    }

    public async Task<(int TotalCount, IList<MexicoProductServiceDto> Items)> SearchProductServicesAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.MexicoProductServices.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => 
                    x.Code.Contains(searchText) || 
                    x.Description.Contains(searchText));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<MexicoProductServiceDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SAT product services");
            throw;
        }
    }
    #endregion

    #region Validation Methods
    public async Task<bool> ValidateUniqueCodeAsync<T>(string code, int? excludeId = null) where T : class
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var query = _context.Set<T>().AsNoTracking();
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => EF.Property<int>(e, "Id") != excludeId.Value);
            }

            return !await query.AnyAsync(e => EF.Property<string>(e, "Code") == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating unique code");
            throw;
        }
    }
    #endregion
}