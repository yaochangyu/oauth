using Microsoft.AspNetCore.Mvc;
using OAuth.Admin.WebAPI.Models;
using OAuth.Admin.WebAPI.Services;

namespace OAuth.Admin.WebAPI.Controllers;

[ApiController]
[Route("api/v1/admin/audit-logs")]
public class AuditLogController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogEntry>>> GetAuditLogs(
        [FromQuery] string? eventType = null,
        [FromQuery] string? actor = null,
        [FromQuery] string? target = null)
    {
        var logs = await auditLogService.QueryAsync(eventType, actor, target);
        return Ok(logs);
    }

    [HttpPost]
    public async Task<IActionResult> RecordAuditLog([FromBody] AuditLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.EventType))
            return BadRequest(new { message = "EventType is required" });

        if (string.IsNullOrEmpty(entry.Actor))
            entry.Actor = User.Identity?.Name ?? "system";

        if (string.IsNullOrEmpty(entry.IpAddress))
            entry.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (entry.Timestamp == default)
            entry.Timestamp = DateTimeOffset.UtcNow;

        await auditLogService.RecordAsync(entry);
        return Ok(new { message = "Audit log recorded successfully" });
    }
}
