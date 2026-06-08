using System.Text.Json;

using App.Core.DTOs.Shared;
using App.Core.Enums.Shared;
using App.Core.Interfaces;
using App.Models.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Shared;

public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<AuditLogService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuditLogService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<AuditLogService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<(int TotalCount, IList<AuditLogDto> Items)> GetAsync(
        int page = 1,
        int pageSize = 20,
        string? entityType = null,
        string? userId = null,
        AuditAction? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserName == userId);

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (fromUtc.HasValue)
            query = query.Where(a => a.Timestamp >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.Timestamp <= toUtc.Value);

        var totalCount = await query.CountAsync();

        // Resolve the stored user Id to a friendly name without requiring an inverse FK.
        var rawItems = await query
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GroupJoin(
                context.Users.IgnoreQueryFilters(),
                a => a.UserName,
                u => u.Id,
                (a, users) => new { Log = a, UserFullName = users.Select(u => u.FullName).FirstOrDefault() })
            .ToListAsync();

        var items = rawItems.Select(x => new AuditLogDto
        {
            Id = x.Log.Id,
            EntityType = x.Log.EntityType,
            TableName = x.Log.TableName,
            EntityId = x.Log.EntityId,
            Action = x.Log.Action,
            Changes = ParseChanges(x.Log.Changes),
            UserId = x.Log.UserName,
            UserName = string.IsNullOrWhiteSpace(x.UserFullName) ? x.Log.UserName : x.UserFullName!,
            Timestamp = x.Log.Timestamp
        }).ToList();

        return (totalCount, items);
    }

    public async Task<IList<string>> GetEntityTypesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<IList<AuditUserDto>> GetUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var userIds = await context.AuditLogs
            .AsNoTracking()
            .Select(a => a.UserName)
            .Distinct()
            .ToListAsync();

        var names = await context.Users
            .IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();

        var nameMap = names.ToDictionary(u => u.Id, u => u.FullName);

        return userIds
            .Select(id => new AuditUserDto
            {
                Id = id,
                Name = nameMap.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : id
            })
            .OrderBy(u => u.Name)
            .ToList();
    }

    private List<AuditChangeDto> ParseChanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<AuditChangeDto>>(json, _jsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse audit log changes JSON");
            return [];
        }
    }
}
