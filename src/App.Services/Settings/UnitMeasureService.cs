using AutoMapper;

using App.Core.DTOs.UnitMeasure;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Products;

public class UnitMeasureService : IUnitMeasureService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<UnitMeasureService> _logger;
    private readonly IStringLocalizer<UnitMeasureService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public UnitMeasureService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<UnitMeasureService> logger,
        IStringLocalizer<UnitMeasureService> localizer,
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

    public async Task<IList<UnitMeasureDto>> GetActiveUnitMeasuresAsync(string countryCode)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var unitMeasures = await _context.UnitMeasures
                .Include(u => u.MexicoSatUnit)
                .AsNoTracking()
                .Where(x => x.CountryCode == countryCode)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<UnitMeasureDto>(x))
                .ToListAsync();

            return unitMeasures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unit measures for country {CountryCode}", countryCode);
            throw;
        }
    }

    public async Task<UnitMeasureDto?> GetUnitMeasureByIdAsync(int id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var unitMeasure = await _context.UnitMeasures
                .Include(u => u.MexicoSatUnit)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return unitMeasure != null ? _mapper.Map<UnitMeasureDto>(unitMeasure) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unit measure by id {Id}", id);
            throw;
        }
    }

    public async Task<(int TotalCount, IList<UnitMeasureDto> Items)> GetUnitMeasuresAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? countryCode = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            IQueryable<UnitMeasure> query = _context.UnitMeasures
                .Include(u => u.MexicoSatUnit)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    x.Code.Contains(searchString) ||
                    (x.Description != null && x.Description.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                query = query.Where(x => x.CountryCode == countryCode);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(x => x.CountryCode)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<UnitMeasureDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unit measures");
            throw;
        }
    }

    public async Task<UnitMeasureDto> CreateUnitMeasureAsync(CreateUnitMeasureDto createDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Check if unit measure with same code already exists for the country
            var exists = await _context.UnitMeasures
                .AsNoTracking()
                .AnyAsync(x => x.Code == createDto.Code && x.CountryCode == createDto.CountryCode);

            if (exists)
            {
                throw new InvalidOperationException(
                    _localizer["Unit measure with code {0} already exists for country {1}", 
                    createDto.Code, createDto.CountryCode]);
            }

            var unitMeasure = _mapper.Map<UnitMeasure>(createDto);

            // Set audit fields
            unitMeasure.CreatedBy = await _currentUserService.GetFullNameAsync();
            unitMeasure.CreatedAt = _dateTime.Now;

            _context.UnitMeasures.Add(unitMeasure);
            await _context.SaveChangesAsync();

            return _mapper.Map<UnitMeasureDto>(unitMeasure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unit measure");
            throw;
        }
    }

    public async Task<UnitMeasureDto> UpdateUnitMeasureAsync(int id, UpdateUnitMeasureDto updateDto)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var unitMeasure = await _context.UnitMeasures
                .FirstOrDefaultAsync(x => x.Id == id);

            if (unitMeasure == null)
            {
                throw new InvalidOperationException(
                    _localizer["Unit measure not found with ID {0}", id]);
            }

            // Check if code is being changed and if new one already exists for this country
            if (updateDto.Code != unitMeasure.Code || updateDto.CountryCode != unitMeasure.CountryCode)
            {
                var exists = await _context.UnitMeasures
                    .AnyAsync(x => x.Id != id && 
                              x.Code == updateDto.Code && 
                              x.CountryCode == updateDto.CountryCode);

                if (exists)
                {
                    throw new InvalidOperationException(
                        _localizer["Unit measure with code {0} already exists for country {1}", 
                        updateDto.Code, updateDto.CountryCode]);
                }
            }

            // Update properties
            _mapper.Map(updateDto, unitMeasure);

            // Update audit fields
            unitMeasure.ModifiedBy = await _currentUserService.GetFullNameAsync();
            unitMeasure.ModifiedAt = _dateTime.Now;

            await _context.SaveChangesAsync();

            return _mapper.Map<UnitMeasureDto>(unitMeasure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit measure {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteUnitMeasureAsync(int id)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var unitMeasure = await _context.UnitMeasures
                .FirstOrDefaultAsync(x => x.Id == id);

            if (unitMeasure == null)
            {
                return false;
            }

            // Check if unit measure has related records in products
            var hasRelatedRecords = await _context.Products
                .AnyAsync(x => x.UnitMeasureId == id);

            if (hasRelatedRecords)
            {
                throw new InvalidOperationException(
                    _localizer["Cannot delete unit measure because it is being used by products"]);
            }

            unitMeasure.DeletedBy = await _currentUserService.GetFullNameAsync();
            unitMeasure.DeletedAt = _dateTime.Now;
            _context.UnitMeasures.Remove(unitMeasure);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting unit measure {Id}", id);
            throw;
        }
    }

    public async Task<bool> ValidateUniqueCodeAsync(string code, string countryCode, int? excludeId = null)
    {
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var query = _context.UnitMeasures.AsNoTracking();
            
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(x => x.Code == code && x.CountryCode == countryCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating unit measure code uniqueness");
            throw;
        }
    }
}