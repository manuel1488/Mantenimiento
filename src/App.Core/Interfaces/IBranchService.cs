using App.Core.Common;
using App.Core.DTOs.Branch;

namespace App.Core.Interfaces;

public interface IBranchService
{
    Task<Result<(int TotalCount, IList<BranchDto> Items)>> GetBranchesAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        bool? isActive = null);

    Task<Result<BranchDto>> GetBranchByIdAsync(int id);

    Task<Result<BranchDto>> CreateBranchAsync(CreateBranchDto createDto);

    Task<Result<BranchDto>> UpdateBranchAsync(int id, UpdateBranchDto updateDto);

    Task<Result> DeleteBranchAsync(int id);

    Task<Result<bool>> ValidateUniqueNameAsync(string name, int? excludeId = null);

    Task<Result<IList<BranchDto>>> GetActiveBranchesAsync();

    Task<Result<IList<BranchDto>>> GetBranchesForUserAsync(string userId);
}
