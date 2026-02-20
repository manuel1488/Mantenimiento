namespace App.Shared.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string? UserName { get; }
    string FullName { get; }

    // Location context
    int? ActiveLocationId { get; }
    bool IsGlobalAccess { get; }
    Task EnsureInitializedAsync();
    Task<IReadOnlyList<int>> GetAssignedLocationIdsAsync();
    Task SetActiveLocationAsync(int? locationId);
    Task<bool> HasAccessToLocationAsync(int locationId);
}