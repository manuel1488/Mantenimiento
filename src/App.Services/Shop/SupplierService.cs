using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class SupplierService : ISupplierService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<SupplierService> _logger;
    private readonly IStringLocalizer<SupplierService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public SupplierService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<SupplierService> logger,
        IStringLocalizer<SupplierService> localizer,
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

    public async Task<(int TotalCount, IList<SupplierDto> Items)> GetSuppliersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Supplier> query = context.Suppliers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    (x.TaxId != null && x.TaxId.Contains(searchString)) ||
                    (x.Email != null && x.Email.Contains(searchString)) ||
                    (x.City != null && x.City.Contains(searchString)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<SupplierDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suppliers");
            throw;
        }
    }

    public async Task<IList<SupplierDto>> GetActiveSuppliersAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Suppliers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<SupplierDto>(x))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active suppliers");
            throw;
        }
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var supplier = await context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return supplier != null ? _mapper.Map<SupplierDto>(supplier) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier by id {Id}", id);
            throw;
        }
    }

    public async Task<Result<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (!string.IsNullOrEmpty(dto.TaxId))
            {
                var exists = await context.Suppliers
                    .AnyAsync(x => x.TaxId == dto.TaxId && x.CountryCode == dto.CountryCode);

                if (exists)
                    return Result<SupplierDto>.Failure(
                        _localizer["Supplier with tax ID {0} already exists", dto.TaxId]);
            }

            var supplier = _mapper.Map<Supplier>(dto);
            supplier.CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            supplier.CreatedAt = _dateTime.Now;

            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            return Result<SupplierDto>.Success(_mapper.Map<SupplierDto>(supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return Result<SupplierDto>.Failure(_localizer["Error creating supplier"]);
        }
    }

    public async Task<Result<SupplierDto>> UpdateSupplierAsync(long id, UpdateSupplierDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var supplier = await context.Suppliers.FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return Result<SupplierDto>.Failure(_localizer["Supplier not found with ID {0}", id]);

            if (!string.IsNullOrEmpty(dto.TaxId) && dto.TaxId != supplier.TaxId)
            {
                var exists = await context.Suppliers
                    .AnyAsync(x => x.Id != id && x.TaxId == dto.TaxId && x.CountryCode == dto.CountryCode);

                if (exists)
                    return Result<SupplierDto>.Failure(
                        _localizer["Supplier with tax ID {0} already exists", dto.TaxId]);
            }

            _mapper.Map(dto, supplier);
            supplier.ModifiedBy = await _currentUserService.GetUserIdAsync();
            supplier.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return Result<SupplierDto>.Success(_mapper.Map<SupplierDto>(supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {Id}", id);
            return Result<SupplierDto>.Failure(_localizer["Error updating supplier"]);
        }
    }

    public async Task<Result> DeleteSupplierAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var supplier = await context.Suppliers.FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return Result.Failure(_localizer["Supplier not found with ID {0}", id]);

            var hasMovements = await context.InventoryMovements
                .AnyAsync(x => x.SupplierId == id);

            if (hasMovements)
                return Result.Failure(_localizer["Cannot delete supplier because it has related inventory movements"]);

            supplier.DeletedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            context.Suppliers.Remove(supplier);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier {Id}", id);
            return Result.Failure(_localizer["Error deleting supplier"]);
        }
    }
}
