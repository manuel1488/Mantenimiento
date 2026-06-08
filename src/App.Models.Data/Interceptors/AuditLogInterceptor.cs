using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

using App.Core.Attributes;
using App.Core.Enums.Shared;
using App.Core.Interfaces;
using App.Models.Shared;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Models.Data.Interceptors;

/// <summary>
/// Records the full change history (old -> new values) for entities marked with
/// <see cref="IAuditTracked"/>. Works in two phases:
/// - SavingChanges: captures the diff (lost after save) and classifies the action.
/// - SavedChanges: resolves identity primary keys for inserts, then persists the logs.
///
/// Must be registered AFTER <see cref="AuditableEntityInterceptor"/> so that soft-deletes
/// (Deleted -> Modified + IsDeleted version bump) are already applied and can be detected.
/// Reads the responsible user directly from the entity's CreatedBy/ModifiedBy/DeletedBy.
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IDateTime _dateTime;

    // One interceptor instance is shared across all contexts (singleton options), so
    // per-operation state is keyed by the DbContext rather than stored in a field.
    private static readonly ConditionalWeakTable<DbContext, List<PendingAudit>> _pending = new();

    // Cache of (entity type, property name) -> whether the property is [SensitiveData].
    private static readonly ConcurrentDictionary<(Type, string), bool> _sensitiveCache = new();

    // Placeholder written instead of a secret value (passwords, tokens, keys).
    private const string RedactedValue = "********";

    // Audit metadata columns are tracked via UserName/Timestamp/Action, not in the diff.
    private static readonly HashSet<string> _ignoredProperties = new()
    {
        nameof(IAuditableEntity.CreatedBy),
        nameof(IAuditableEntity.CreatedAt),
        nameof(IAuditableEntity.ModifiedBy),
        nameof(IAuditableEntity.ModifiedAt),
        nameof(ISoftDelete.DeletedBy),
        nameof(ISoftDelete.DeletedAt),
        nameof(ISoftDelete.IsDeleted)
    };

    public AuditLogInterceptor(IDateTime dateTime)
    {
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Persist(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PersistAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context == null) return;

        var pending = new List<PendingAudit>();

        foreach (var entry in context.ChangeTracker.Entries<IAuditTracked>())
        {
            var action = Classify(entry);
            if (action == null) continue;

            var changes = action == AuditAction.Update ? BuildChanges(entry) : null;

            // Skip Updates where nothing meaningful changed (only audit metadata).
            if (action == AuditAction.Update && string.IsNullOrEmpty(changes))
                continue;

            pending.Add(new PendingAudit
            {
                Entry = entry,
                EntityType = entry.Metadata.ClrType.Name,
                TableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
                Action = action.Value,
                Changes = changes,
                UserName = ResolveUser(entry.Entity, action.Value),
                // Insert PKs are store-generated and unknown until after save.
                ResolveKeyAfterSave = action == AuditAction.Insert,
                EntityId = action == AuditAction.Insert ? null : GetPrimaryKey(entry)
            });
        }

        if (pending.Count > 0)
        {
            _pending.Remove(context);
            _pending.Add(context, pending);
        }
    }

    private void Persist(DbContext? context)
    {
        var logs = DrainPending(context);
        if (logs == null) return;

        context!.Set<AuditLog>().AddRange(logs);
        context.SaveChanges();
    }

    private async Task PersistAsync(DbContext? context, CancellationToken cancellationToken)
    {
        var logs = DrainPending(context);
        if (logs == null) return;

        await context!.Set<AuditLog>().AddRangeAsync(logs, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Builds the AuditLog rows for a completed save and clears pending state for the
    /// context (so the recursive SaveChanges that persists the logs is not re-processed).
    /// </summary>
    private List<AuditLog>? DrainPending(DbContext? context)
    {
        if (context == null) return null;
        if (!_pending.TryGetValue(context, out var pending)) return null;

        _pending.Remove(context);

        var now = _dateTime.Now;
        var logs = new List<AuditLog>(pending.Count);

        foreach (var item in pending)
        {
            logs.Add(new AuditLog
            {
                EntityType = item.EntityType,
                TableName = item.TableName,
                EntityId = item.ResolveKeyAfterSave ? GetPrimaryKey(item.Entry) : item.EntityId!,
                Action = item.Action,
                Changes = item.Changes,
                UserName = item.UserName,
                Timestamp = now
            });
        }

        return logs;
    }

    private static AuditAction? Classify(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                return AuditAction.Insert;

            case EntityState.Deleted:
                // Hard delete (no soft-delete conversion applied).
                return AuditAction.Delete;

            case EntityState.Modified:
                // AuditableEntityInterceptor converts soft-deletes to Modified and bumps
                // IsDeleted from 0 to a version counter. Detect that as a Delete.
                if (entry.Entity is ISoftDelete)
                {
                    var isDeleted = entry.Property(nameof(ISoftDelete.IsDeleted));
                    if (isDeleted.IsModified
                        && Convert.ToUInt32(isDeleted.OriginalValue) == 0
                        && Convert.ToUInt32(isDeleted.CurrentValue) != 0)
                    {
                        return AuditAction.Delete;
                    }
                }
                return AuditAction.Update;

            default:
                return null;
        }
    }

    private static string? BuildChanges(EntityEntry entry)
    {
        var changes = new List<PropertyChange>();
        var clrType = entry.Metadata.ClrType;

        foreach (var prop in entry.Properties)
        {
            if (!prop.IsModified) continue;

            var name = prop.Metadata.Name;
            if (_ignoredProperties.Contains(name)) continue;

            var oldValue = Format(prop.OriginalValue);
            var newValue = Format(prop.CurrentValue);

            // Compare actual values so a real change is detected even when redacted.
            if (oldValue == newValue) continue;

            if (IsSensitive(clrType, name))
            {
                // Record that the secret changed, but never store its value.
                oldValue = oldValue == null ? null : RedactedValue;
                newValue = newValue == null ? null : RedactedValue;
            }

            changes.Add(new PropertyChange
            {
                Property = name,
                Old = oldValue,
                New = newValue
            });
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
    }

    private static bool IsSensitive(Type clrType, string propertyName)
    {
        return _sensitiveCache.GetOrAdd((clrType, propertyName), key =>
            key.Item1.GetProperty(key.Item2)
                ?.GetCustomAttribute<SensitiveDataAttribute>() != null);
    }

    private static string ResolveUser(object entity, AuditAction action)
    {
        string? user = action switch
        {
            AuditAction.Insert => (entity as IAuditableEntity)?.CreatedBy,
            AuditAction.Delete => (entity as ISoftDelete)?.DeletedBy
                                  ?? (entity as IAuditableEntity)?.ModifiedBy,
            _ => (entity as IAuditableEntity)?.ModifiedBy
        };

        return string.IsNullOrEmpty(user) ? "System" : user;
    }

    private static string GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null) return string.Empty;

        var values = key.Properties
            .Select(p => Format(entry.Property(p.Name).CurrentValue) ?? string.Empty);

        return string.Join(",", values);
    }

    private static string? Format(object? value)
    {
        return value switch
        {
            null => null,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private class PendingAudit
    {
        public required EntityEntry Entry { get; init; }
        public required string EntityType { get; init; }
        public required string TableName { get; init; }
        public required AuditAction Action { get; init; }
        public string? Changes { get; init; }
        public required string UserName { get; init; }
        public bool ResolveKeyAfterSave { get; init; }
        public string? EntityId { get; init; }
    }

    private class PropertyChange
    {
        public string Property { get; set; } = null!;
        public string? Old { get; set; }
        public string? New { get; set; }
    }
}
