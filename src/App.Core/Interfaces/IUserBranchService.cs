using App.Core.Common;
using App.Core.DTOs.Branch;

namespace App.Core.Interfaces;

public interface IUserBranchService
{
    Task<Result<IList<BranchDto>>> GetUserBranchesAsync(string userId);

    Task<Result> AssignBranchesToUserAsync(string userId, IList<int> branchIds);

    Task<Result> RemoveBranchFromUserAsync(string userId, int branchId);
}
