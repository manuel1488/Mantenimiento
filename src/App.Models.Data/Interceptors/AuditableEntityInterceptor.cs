using System.Runtime.CompilerServices;

using App.Core.Interfaces;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Models.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IDateTime _dateTime;

    public AuditableEntityInterceptor(
        IDateTime dateTime)
    {
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                    throw new InvalidOperationException("CreatedBy is required for new entities");
                
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = _dateTime.Now;
            }
            else if (entry.State == EntityState.Modified)
            {
                 if (string.IsNullOrEmpty(entry.Entity.ModifiedBy))
                    throw new InvalidOperationException("ModifiedBy is required for modified entities");
                
                if (entry.Entity.ModifiedAt == default)
                    entry.Entity.ModifiedAt = _dateTime.Now;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                if (string.IsNullOrEmpty(entry.Entity.DeletedBy))
                    throw new InvalidOperationException("DeletedBy is required for deleted entities");
                
                entry.State = EntityState.Modified;

                // Get the table name
                var entityType = context.Model.FindEntityType(entry.Entity.GetType());
                var tableName = entityType?.GetTableName();
                
                if (tableName == null) continue;

                // Get the max version
                var sql = $"SELECT COALESCE(MAX(`IsDeleted`), 0) MaxVersion FROM `{tableName}`";
                var query = FormattableStringFactory.Create(sql);
                var maxVersion = context.Database
                    .SqlQuery<MaxVersionResult>(query)
                    .First()
                    .MaxVersion;

                // Increment the version
                entry.Entity.IsDeleted = maxVersion + 1;
                
                if (entry.Entity.DeletedAt == default)
                    entry.Entity.DeletedAt = _dateTime.Now;
            }
        }
    }

    private class MaxVersionResult
    {
        public uint MaxVersion { get; set; }
    }
}