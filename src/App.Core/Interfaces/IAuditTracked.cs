namespace App.Core.Interfaces;

/// <summary>
/// Marker interface for entities whose full change history (old -> new values)
/// must be recorded in the audit log. Capture is handled by the AuditLogInterceptor.
/// </summary>
public interface IAuditTracked
{
}
