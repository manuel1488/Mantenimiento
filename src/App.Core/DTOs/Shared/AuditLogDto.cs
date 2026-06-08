using App.Core.Enums.Shared;

namespace App.Core.DTOs.Shared;

/// <summary>
/// A single audit-log entry for display in the admin viewer.
/// </summary>
public class AuditLogDto
{
    public long Id { get; set; }
    public string EntityType { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public AuditAction Action { get; set; }

    /// <summary>Parsed property-level changes (empty for Insert/Delete).</summary>
    public List<AuditChangeDto> Changes { get; set; } = [];

    /// <summary>Raw stored user identifier (the application user Id).</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Friendly user name resolved from the Id; falls back to the raw value.</summary>
    public string UserName { get; set; } = null!;

    /// <summary>UTC timestamp of the change.</summary>
    public DateTime Timestamp { get; set; }
}

public class AuditChangeDto
{
    public string Property { get; set; } = null!;
    public string? Old { get; set; }
    public string? New { get; set; }
}

/// <summary>A user that appears in the audit log, for the viewer filter dropdown.</summary>
public class AuditUserDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
}
