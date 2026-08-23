using App.Core.DTOs.Shared;
using App.Core.Enums.Shared;

namespace App.Core.Interfaces;

/// <summary>
/// Read-only access to the change-history audit log (<c>aud_change_log</c>).
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Returns a page of audit-log entries (newest first) matching the optional filters.
    /// Dates are expected in UTC.
    /// </summary>
    Task<(int TotalCount, IList<AuditLogDto> Items)> GetAsync(
        int page = 1,
        int pageSize = 20,
        string? entityType = null,
        string? entityId = null,
        string? userId = null,
        AuditAction? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null);

    /// <summary>Distinct entity types present in the log, for the filter dropdown.</summary>
    Task<IList<string>> GetEntityTypesAsync();

    /// <summary>Distinct users present in the log (Id + resolved name), for the filter dropdown.</summary>
    Task<IList<AuditUserDto>> GetUsersAsync();
}
