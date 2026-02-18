using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Branch;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Identity;

public class UserBranchService : IUserBranchService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<UserBranchService> _logger;
    private readonly IStringLocalizer<UserBranchService> L;

    public UserBranchService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<UserBranchService> logger,
        IStringLocalizer<UserBranchService> localizer)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
    }

    public async Task<Result<IList<BranchDto>>> GetUserBranchesAsync(string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var branches = await context.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.Branch)
                .Where(b => b.IsDeleted == 0)
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

    public async Task<Result> AssignBranchesToUserAsync(string userId, IList<int> branchIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Remove existing assignments
            var existing = await context.UserBranches
                .Where(ub => ub.UserId == userId)
                .ToListAsync();

            context.UserBranches.RemoveRange(existing);

            // Add new assignments
            foreach (var branchId in branchIds)
            {
                context.UserBranches.Add(new UserBranch
                {
                    UserId = userId,
                    BranchId = branchId
                });
            }

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning branches to user {UserId}", userId);
            return Result.Failure(L["Error assigning branches to user"]);
        }
    }

    public async Task<Result> RemoveBranchFromUserAsync(string userId, int branchId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var assignment = await context.UserBranches
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == branchId);

            if (assignment == null)
                return Result.Failure(L["Branch assignment not found"]);

            context.UserBranches.Remove(assignment);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing branch {BranchId} from user {UserId}", branchId, userId);
            return Result.Failure(L["Error removing branch from user"]);
        }
    }
}
