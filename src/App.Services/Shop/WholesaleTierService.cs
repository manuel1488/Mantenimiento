using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class WholesaleTierService : IWholesaleTierService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<WholesaleTierService> _logger;
    private readonly IStringLocalizer<WholesaleTierService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public WholesaleTierService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<WholesaleTierService> logger,
        IStringLocalizer<WholesaleTierService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<IList<WholesaleTierDto>>> GetActiveTiersAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tiers = await context.WholesaleTiers
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var dtos = _mapper.Map<IList<WholesaleTierDto>>(tiers);
            return Result<IList<WholesaleTierDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active wholesale tiers");
            return Result<IList<WholesaleTierDto>>.Failure(L["Error retrieving wholesale tiers"]);
        }
    }

    public async Task<Result<IList<WholesaleTierDto>>> GetAllTiersAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tiers = await context.WholesaleTiers
                .AsNoTracking()
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var dtos = _mapper.Map<IList<WholesaleTierDto>>(tiers);
            return Result<IList<WholesaleTierDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all wholesale tiers");
            return Result<IList<WholesaleTierDto>>.Failure(L["Error retrieving wholesale tiers"]);
        }
    }

    public async Task<Result<WholesaleTierDto>> GetTierByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tier = await context.WholesaleTiers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tier == null)
                return Result<WholesaleTierDto>.Failure(L["Wholesale tier not found"]);

            var dto = _mapper.Map<WholesaleTierDto>(tier);
            return Result<WholesaleTierDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wholesale tier {Id}", id);
            return Result<WholesaleTierDto>.Failure(L["Error retrieving wholesale tier"]);
        }
    }

    public async Task<Result<WholesaleTierDto>> CreateTierAsync(CreateWholesaleTierDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Validate unique name
            var existingName = await context.WholesaleTiers
                .AnyAsync(t => t.Name == dto.Name);

            if (existingName)
                return Result<WholesaleTierDto>.Failure(L["Wholesale tier name already exists"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            var tier = new WholesaleTier
            {
                Name = dto.Name,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive,
                CreatedBy = currentUser,
                CreatedAt = currentTime,
                ModifiedBy = currentUser,
                ModifiedAt = currentTime,
                IsDeleted = 0
            };

            context.WholesaleTiers.Add(tier);
            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<WholesaleTierDto>(tier);
            return Result<WholesaleTierDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wholesale tier");
            return Result<WholesaleTierDto>.Failure(L["Error creating wholesale tier"]);
        }
    }

    public async Task<Result<WholesaleTierDto>> UpdateTierAsync(int id, UpdateWholesaleTierDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tier = await context.WholesaleTiers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tier == null)
                return Result<WholesaleTierDto>.Failure(L["Wholesale tier not found"]);

            // Validate unique name (excluding current)
            var existingName = await context.WholesaleTiers
                .AnyAsync(t => t.Name == dto.Name && t.Id != id);

            if (existingName)
                return Result<WholesaleTierDto>.Failure(L["Wholesale tier name already exists"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            tier.Name = dto.Name;
            tier.DisplayOrder = dto.DisplayOrder;
            tier.IsActive = dto.IsActive;
            tier.ModifiedBy = currentUser;
            tier.ModifiedAt = currentTime;

            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<WholesaleTierDto>(tier);
            return Result<WholesaleTierDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wholesale tier {Id}", id);
            return Result<WholesaleTierDto>.Failure(L["Error updating wholesale tier"]);
        }
    }

    public async Task<Result> DeleteTierAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tier = await context.WholesaleTiers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tier == null)
                return Result.Failure(L["Wholesale tier not found"]);

            // Check if tier is in use
            var isInUse = await context.ProductWholesalePrices
                .AnyAsync(p => p.WholesaleTierId == id);

            if (isInUse)
                return Result.Failure(L["Cannot delete tier that is in use by products"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            tier.IsDeleted = 1;
            tier.ModifiedBy = currentUser;
            tier.ModifiedAt = currentTime;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting wholesale tier {Id}", id);
            return Result.Failure(L["Error deleting wholesale tier"]);
        }
    }

    public async Task<Result> ToggleActiveAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tier = await context.WholesaleTiers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tier == null)
                return Result.Failure(L["Wholesale tier not found"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            tier.IsActive = !tier.IsActive;
            tier.ModifiedBy = currentUser;
            tier.ModifiedAt = currentTime;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling wholesale tier status {Id}", id);
            return Result.Failure(L["Error updating wholesale tier status"]);
        }
    }

    public async Task<Result<bool>> ValidateUniqueNameAsync(string name, int? excludeId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.WholesaleTiers.AsQueryable();

            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            var exists = await query.AnyAsync(t => t.Name == name);

            return Result<bool>.Success(!exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating wholesale tier name");
            return Result<bool>.Failure(L["Error validating wholesale tier name"]);
        }
    }
}
