namespace App.Shared.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string? UserName { get; }
    string FullName { get; }

    // Branch context
    int? ActiveBranchId { get; }
    bool IsGlobalAccess { get; }
    Task<IReadOnlyList<int>> GetAssignedBranchIdsAsync();
    Task SetActiveBranchAsync(int? branchId);
    Task<bool> HasAccessToBranchAsync(int branchId);
}