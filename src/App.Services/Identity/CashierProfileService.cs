using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Identity;
using App.Core.Interfaces.Identity;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Identity;

public class CashierProfileService : ICashierProfileService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CashierProfileService> _logger;
    private readonly IStringLocalizer<CashierProfileService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public CashierProfileService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CashierProfileService> logger,
        IStringLocalizer<CashierProfileService> localizer,
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

    public async Task<CashierProfileDto?> GetByUserIdAsync(string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var profile = await context.CashierProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            return profile == null ? null : _mapper.Map<CashierProfileDto>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cashier profile for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> IsActiveCashierAsync(string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CashierProfiles
                .AnyAsync(c => c.UserId == userId && c.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cashier status for user {UserId}", userId);
            return false;
        }
    }

    public async Task<(int Total, IList<CashierProfileDto> Items)> GetAllAsync(int page, int pageSize, bool? isActive = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.CashierProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Location)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<CashierProfileDto>>(items);
            return (total, dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cashier profiles");
            return (0, []);
        }
    }

    public async Task<Result<CashierProfileDto>> CreateAsync(CreateCashierProfileDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var alreadyExists = await context.CashierProfiles
                .AnyAsync(c => c.UserId == dto.UserId);

            if (alreadyExists)
                return Result<CashierProfileDto>.Failure(_localizer["A cashier profile already exists for this user"]);

            if (dto.LocationId <= 0)
                return Result<CashierProfileDto>.Failure(_localizer["Location is required"]);

            var now = _dateTime.Now;
            var currentUser = await _currentUserService.GetFullNameAsync();

            var profile = new CashierProfile
            {
                UserId = dto.UserId,
                LocationId = dto.LocationId,
                IsActive = true,
                Notes = dto.Notes,
                CreatedBy = currentUser,
                CreatedAt = now,
                ModifiedBy = currentUser,
                ModifiedAt = now
            };

            context.CashierProfiles.Add(profile);
            await context.SaveChangesAsync();

            var saved = await context.CashierProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Location)
                .FirstAsync(c => c.Id == profile.Id);

            var resultDto = _mapper.Map<CashierProfileDto>(saved);
            return Result<CashierProfileDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cashier profile for user {UserId}", dto.UserId);
            return Result<CashierProfileDto>.Failure(_localizer["Error creating cashier profile"]);
        }
    }

    public async Task<Result<CashierProfileDto>> UpdateAsync(long id, UpdateCashierProfileDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var profile = await context.CashierProfiles
                .Include(c => c.User)
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (profile == null)
                return Result<CashierProfileDto>.Failure(_localizer["Cashier profile not found"]);

            if (dto.LocationId > 0)
                profile.LocationId = dto.LocationId;
            profile.IsActive = dto.IsActive;
            profile.Notes = dto.Notes;
            profile.ModifiedBy = await _currentUserService.GetFullNameAsync();
            profile.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<CashierProfileDto>(profile);
            return Result<CashierProfileDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cashier profile {Id}", id);
            return Result<CashierProfileDto>.Failure(_localizer["Error updating cashier profile"]);
        }
    }

    public async Task<Result> DeleteAsync(long id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var profile = await context.CashierProfiles
                .FirstOrDefaultAsync(c => c.Id == id);

            if (profile == null)
                return Result.Failure(_localizer["Cashier profile not found"]);

            context.CashierProfiles.Remove(profile);
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cashier profile {Id}", id);
            return Result.Failure(_localizer["Error deleting cashier profile"]);
        }
    }
}
