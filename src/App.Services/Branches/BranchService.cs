using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Branch;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Branches;

public class BranchService : IBranchService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<BranchService> _logger;
    private readonly IStringLocalizer<BranchService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public BranchService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<BranchService> logger,
        IStringLocalizer<BranchService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Result<(int TotalCount, IList<BranchDto> Items)>> GetBranchesAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            IQueryable<Branch> query = context.Branches.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    (x.Description != null && x.Description.Contains(searchString)) ||
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
                .Select(x => _mapper.Map<BranchDto>(x))
                .ToListAsync();

            return Result<(int, IList<BranchDto>)>.Success((totalCount, items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches");
            return Result<(int, IList<BranchDto>)>.Failure(L["Error retrieving branches"]);
        }
    }

    public async Task<Result<BranchDto>> GetBranchByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var branch = await context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (branch == null)
                return Result<BranchDto>.Failure(L["Branch not found"]);

            return Result<BranchDto>.Success(_mapper.Map<BranchDto>(branch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch {Id}", id);
            return Result<BranchDto>.Failure(L["Error retrieving branch"]);
        }
    }

    public async Task<Result<BranchDto>> CreateBranchAsync(CreateBranchDto createDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var exists = await context.Branches
                .AnyAsync(x => x.Name == createDto.Name);

            if (exists)
                return Result<BranchDto>.Failure(L["Branch with name {0} already exists", createDto.Name]);

            var branch = _mapper.Map<Branch>(createDto);

            branch.CreatedBy = _currentUserService.FullName ?? "Unknown";
            branch.CreatedAt = _dateTime.Now;

            context.Branches.Add(branch);
            await context.SaveChangesAsync();

            return Result<BranchDto>.Success(_mapper.Map<BranchDto>(branch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
            return Result<BranchDto>.Failure(L["Error creating branch"]);
        }
    }

    public async Task<Result<BranchDto>> UpdateBranchAsync(int id, UpdateBranchDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var branch = await context.Branches
                .FirstOrDefaultAsync(x => x.Id == id);

            if (branch == null)
                return Result<BranchDto>.Failure(L["Branch not found"]);

            if (updateDto.Name != branch.Name)
            {
                var exists = await context.Branches
                    .AnyAsync(x => x.Id != id && x.Name == updateDto.Name);

                if (exists)
                    return Result<BranchDto>.Failure(L["Branch with name {0} already exists", updateDto.Name]);
            }

            _mapper.Map(updateDto, branch);

            branch.ModifiedBy = _currentUserService.FullName ?? "Unknown";
            branch.ModifiedAt = _dateTime.Now;

            await context.SaveChangesAsync();

            return Result<BranchDto>.Success(_mapper.Map<BranchDto>(branch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating branch {Id}", id);
            return Result<BranchDto>.Failure(L["Error updating branch"]);
        }
    }

    public async Task<Result> DeleteBranchAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var branch = await context.Branches
                .FirstOrDefaultAsync(x => x.Id == id);

            if (branch == null)
                return Result.Failure(L["Branch not found"]);

            // Check if branch has associated warehouses
            var hasWarehouses = await context.Warehouses
                .AnyAsync(x => x.BranchId == id);

            if (hasWarehouses)
                return Result.Failure(L["Cannot delete branch because it has associated warehouses"]);

            // Check if branch has associated sales
            var hasSales = await context.Sales
                .AnyAsync(x => x.BranchId == id);

            if (hasSales)
                return Result.Failure(L["Cannot delete branch because it has associated sales"]);

            branch.DeletedBy = _currentUserService.FullName ?? "Unknown";
            branch.DeletedAt = _dateTime.Now;
            branch.IsDeleted = 1;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting branch {Id}", id);
            return Result.Failure(L["Error deleting branch"]);
        }
    }

    public async Task<Result<bool>> ValidateUniqueNameAsync(string name, int? excludeId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.Branches.AsNoTracking();

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            var isUnique = !await query.AnyAsync(x => x.Name == name);
            return Result<bool>.Success(isUnique);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating branch name uniqueness");
            return Result<bool>.Failure(L["Error validating branch name"]);
        }
    }

    public async Task<Result<IList<BranchDto>>> GetActiveBranchesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var branches = await context.Branches
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<BranchDto>(x))
                .ToListAsync();

            return Result<IList<BranchDto>>.Success(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active branches");
            return Result<IList<BranchDto>>.Failure(L["Error retrieving branches"]);
        }
    }

    public async Task<Result<IList<BranchDto>>> GetBranchesForUserAsync(string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var branches = await context.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.Branch)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => _mapper.Map<BranchDto>(b))
                .ToListAsync();

            return Result<IList<BranchDto>>.Success(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches for user {UserId}", userId);
            return Result<IList<BranchDto>>.Failure(L["Error retrieving user branches"]);
        }
    }
}
