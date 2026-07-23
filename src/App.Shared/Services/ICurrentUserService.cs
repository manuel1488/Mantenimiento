namespace App.Shared.Services;

public interface ICurrentUserService
{
    Task<string> GetUserIdAsync();
    Task<string?> GetUserNameAsync();
    Task<string?> GetFullNameAsync();

    // Location context
    Task<int?> GetActiveLocationIdAsync();
    Task<bool> GetIsGlobalAccessAsync();
    Task EnsureInitializedAsync();
    Task<IReadOnlyList<int>> GetAssignedLocationIdsAsync();
    Task SetActiveLocationAsync(int? locationId);
    Task<bool> HasAccessToLocationAsync(int locationId);
}
