using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Enums.Shared;

namespace App.Models.Shared;

/// <summary>
/// Records the full change history (old -> new values) for sensitive entities
/// marked with <see cref="App.Core.Interfaces.IAuditTracked"/>.
/// Populated automatically by the AuditLogInterceptor — never written by services.
/// This entity is intentionally NOT auditable/soft-deletable: it is an append-only log.
/// </summary>
[Table("aud_change_log")]
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>
    /// CLR type name of the audited entity (e.g. "Product").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// Database table name of the audited entity (e.g. "sh_products").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string TableName { get; set; } = null!;

    /// <summary>
    /// Primary key of the audited row, as string (composite keys joined by ",").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityId { get; set; } = null!;

    public AuditAction Action { get; set; }

    /// <summary>
    /// JSON array of changed properties: [{ "Property", "Old", "New" }].
    /// Null/empty for Insert and Delete (the row state is captured by EntityId/Action).
    /// </summary>
    [Column(TypeName = "json")]
    public string? Changes { get; set; }

    /// <summary>
    /// User responsible for the change, read from the entity's CreatedBy/ModifiedBy/DeletedBy.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = null!;

    public DateTime Timestamp { get; set; }
}
