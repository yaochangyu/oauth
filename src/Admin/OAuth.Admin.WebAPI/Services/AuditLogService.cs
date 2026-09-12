using System.Collections.Concurrent;
using OAuth.Admin.WebAPI.Models;

namespace OAuth.Admin.WebAPI.Services;

public interface IAuditLogService
{
    Task RecordAsync(AuditLogEntry entry);
    Task<IEnumerable<AuditLogEntry>> QueryAsync(string? eventType = null, string? actor = null, string? target = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly ConcurrentBag<AuditLogEntry> _logs = new();

    public Task RecordAsync(AuditLogEntry entry)
    {
        _logs.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLogEntry>> QueryAsync(string? eventType = null, string? actor = null, string? target = null)
    {
        var query = _logs.AsEnumerable();
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(l => l.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(actor))
            query = query.Where(l => l.Actor.Contains(actor, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(target))
            query = query.Where(l => l.Target.Contains(target, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(query.OrderByDescending(l => l.Timestamp).AsEnumerable());
    }
}
