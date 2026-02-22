using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Shop.CashStation;
using App.Core.Enums.Shop;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class CashStationService : ICashStationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CashStationService> _logger;
    private readonly IStringLocalizer<CashStationService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public CashStationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CashStationService> logger,
        IStringLocalizer<CashStationService> localizer,
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

    public async Task<IList<CashStationDto>> GetByLocationAsync(int locationId, bool? isActive = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.CashStations
                .AsNoTracking()
                .Include(s => s.Location)
                .Where(s => s.LocationId == locationId);

            if (isActive.HasValue)
                query = query.Where(s => s.IsActive == isActive.Value);

            var stations = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            return _mapper.Map<List<CashStationDto>>(stations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash stations for location {LocationId}", locationId);
            return [];
        }
    }

    public async Task<(int Total, IList<CashStationDto> Items)> GetAllAsync(int page, int pageSize, int? locationId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.CashStations
                .AsNoTracking()
                .Include(s => s.Location)
                .AsQueryable();

            if (locationId.HasValue)
                query = query.Where(s => s.LocationId == locationId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.Location.Name)
                .ThenBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, _mapper.Map<List<CashStationDto>>(items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash stations");
            return (0, []);
        }
    }

    public async Task<Result<CashStationDto>> CreateAsync(CreateCashStationDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var nameExists = await context.CashStations
                .AnyAsync(s => s.LocationId == dto.LocationId && s.Name == dto.Name);

            if (nameExists)
                return Result<CashStationDto>.Failure(_localizer["A cash station with this name already exists at this location"]);

            var now = _dateTime.Now;
            var currentUser = _currentUserService.FullName;

            var station = new CashStation
            {
                LocationId = dto.LocationId,
                Name = dto.Name,
                IsActive = true,
                CreatedBy = currentUser,
                CreatedAt = now,
                ModifiedBy = currentUser,
                ModifiedAt = now
            };

            context.CashStations.Add(station);
            await context.SaveChangesAsync();

            var saved = await context.CashStations
                .AsNoTracking()
                .Include(s => s.Location)
                .FirstAsync(s => s.Id == station.Id);

            return Result<CashStationDto>.Success(_mapper.Map<CashStationDto>(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cash station");
            return Result<CashStationDto>.Failure(_localizer["Error creating cash station"]);
        }
    }

    public async Task<Result<CashStationDto>> UpdateAsync(int id, UpdateCashStationDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var station = await context.CashStations
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (station == null)
                return Result<CashStationDto>.Failure(_localizer["Cash station not found"]);

            var nameExists = await context.CashStations
                .AnyAsync(s => s.LocationId == station.LocationId && s.Name == dto.Name && s.Id != id);

            if (nameExists)
                return Result<CashStationDto>.Failure(_localizer["A cash station with this name already exists at this location"]);

            station.Name = dto.Name;
            station.IsActive = dto.IsActive;
            station.ModifiedBy = _currentUserService.FullName;
            station.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return Result<CashStationDto>.Success(_mapper.Map<CashStationDto>(station));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cash station {Id}", id);
            return Result<CashStationDto>.Failure(_localizer["Error updating cash station"]);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var station = await context.CashStations
                .FirstOrDefaultAsync(s => s.Id == id);

            if (station == null)
                return Result.Failure(_localizer["Cash station not found"]);

            var hasOpenRegister = await context.CashRegisters
                .AnyAsync(c => c.CashStationId == id && c.Status == CashRegisterStatus.Open);

            if (hasOpenRegister)
                return Result.Failure(_localizer["Cannot delete a cash station with an open cash register"]);

            context.CashStations.Remove(station);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cash station {Id}", id);
            return Result.Failure(_localizer["Error deleting cash station"]);
        }
    }
}
