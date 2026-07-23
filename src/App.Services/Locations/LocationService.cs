using AutoMapper;
using App.Core.DTOs.Location;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using LocationModel = App.Models.Shop.Location;

namespace App.Services.Locations;

public class LocationService : ILocationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<LocationService> _logger;
    private readonly IStringLocalizer<LocationService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public LocationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<LocationService> logger,
        IStringLocalizer<LocationService> localizer,
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

    public async Task<(int TotalCount, IList<LocationDto> Items)> GetLocationsAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null,
        LocationType? type = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<LocationModel> query = context.Locations.AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    (x.Description != null && x.Description.Contains(searchString)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(x => x.Type == type.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => _mapper.Map<LocationDto>(x))
                .ToListAsync();

            return (totalCount, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations");
            throw;
        }
    }

    public async Task<IList<LocationDto>> GetActiveLocationsAsync(LocationType? type = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<LocationModel> query = context.Locations
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (type.HasValue)
            {
                query = query.Where(x => x.Type == type.Value);
            }

            var locations = await query
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return locations.Select(x => _mapper.Map<LocationDto>(x)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active locations");
            throw;
        }
    }

    public async Task<LocationDto?> GetLocationByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var location = await context.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return location != null ? _mapper.Map<LocationDto>(location) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by id {Id}", id);
            throw;
        }
    }

    public async Task<LocationDto> CreateLocationAsync(CreateLocationDto createDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Check if location with same name already exists
            var exists = await context.Locations
                .AnyAsync(x => x.Name == createDto.Name);

            if (exists)
            {
                throw new InvalidOperationException(
                    _localizer["Location with name {0} already exists", createDto.Name]);
            }

            var location = _mapper.Map<LocationModel>(createDto);

            // Set audit fields
            location.CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            location.CreatedAt = _dateTime.Now;

            context.Locations.Add(location);
            await context.SaveChangesAsync();

            return _mapper.Map<LocationDto>(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            throw;
        }
    }

    public async Task<LocationDto> UpdateLocationAsync(int id, UpdateLocationDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var location = await context.Locations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (location == null)
            {
                throw new InvalidOperationException(
                    _localizer["Location not found with ID {0}", id]);
            }

            // Check if name is being changed and if new one already exists
            if (updateDto.Name != location.Name)
            {
                var exists = await context.Locations
                    .AnyAsync(x => x.Id != id && x.Name == updateDto.Name);

                if (exists)
                {
                    throw new InvalidOperationException(
                        _localizer["Location with name {0} already exists", updateDto.Name]);
                }
            }

            // Update properties
            _mapper.Map(updateDto, location);

            // Update audit fields
            location.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            location.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return _mapper.Map<LocationDto>(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var location = await context.Locations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (location == null)
            {
                return false;
            }

            // Check if location has related inventory records
            var hasInventory = await context.Inventory
                .AnyAsync(x => x.LocationId == id);

            if (hasInventory)
            {
                throw new InvalidOperationException(
                    _localizer["Cannot delete location because it has inventory records"]);
            }

            location.DeletedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            location.DeletedAt = _dateTime.Now;
            location.IsDeleted = 1;

            await context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {Id}", id);
            throw;
        }
    }

    public async Task<bool> ValidateUniqueNameAsync(string name, int? excludeId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.Locations.AsNoTracking();

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(x => x.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating location name uniqueness");
            throw;
        }
    }
}
