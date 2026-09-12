using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Admin.WebAPI.Models;
using OAuth.Admin.WebAPI.Services;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.Admin.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,admin")]
[Route("api/v1/admin/apps")]
public class AppReviewController(
    IOpenIddictApplicationManager appManager,
    IOpenIddictAuthorizationManager authManager,
    IOpenIddictTokenManager tokenManager,
    IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<AppDetailResponse>>> GetPendingApps()
    {
        var pendingApps = new List<AppDetailResponse>();

        await foreach (var app in appManager.ListAsync())
        {
            var detail = await ToDetailResponseAsync(app);
            if (string.Equals(detail.Status, "InReview", StringComparison.OrdinalIgnoreCase))
            {
                pendingApps.Add(detail);
            }
        }

        return Ok(pendingApps);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppDetailResponse>>> GetApps([FromQuery] string? status = null, [FromQuery] string? filter = null)
    {
        var apps = new List<AppDetailResponse>();

        await foreach (var app in appManager.ListAsync())
        {
            var detail = await ToDetailResponseAsync(app);

            if (!string.IsNullOrEmpty(status) && !string.Equals(detail.Status, status, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(filter) &&
                !(detail.ClientId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                  detail.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                  detail.Developer.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                continue;

            apps.Add(detail);
        }

        return Ok(apps);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppDetailResponse>> GetApp(string id)
    {
        var app = await FindAppByIdOrClientIdAsync(id);
        if (app is null) return NotFound(new { message = "Application not found" });

        var detail = await ToDetailResponseAsync(app);
        return Ok(detail);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveApp(string id)
    {
        var app = await FindAppByIdOrClientIdAsync(id);
        if (app is null) return NotFound(new { message = "Application not found" });

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, app);

        descriptor.Properties["status"] = JsonSerializer.SerializeToElement("Approved");
        descriptor.Properties.Remove("rejectReason");

        await appManager.UpdateAsync(app, descriptor);

        var clientId = descriptor.ClientId ?? id;
        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "AppApproved",
            Actor = User.Identity?.Name ?? "admin",
            Target = clientId,
            Details = $"核准應用程式 {clientId} 上線",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Application approved successfully" });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectApp(string id, [FromBody] RejectAppRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Rejection reason is required" });

        var app = await FindAppByIdOrClientIdAsync(id);
        if (app is null) return NotFound(new { message = "Application not found" });

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, app);

        descriptor.Properties["status"] = JsonSerializer.SerializeToElement("Rejected");
        descriptor.Properties["rejectReason"] = JsonSerializer.SerializeToElement(request.Reason);

        await appManager.UpdateAsync(app, descriptor);

        var clientId = descriptor.ClientId ?? id;
        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "AppRejected",
            Actor = User.Identity?.Name ?? "admin",
            Target = clientId,
            Details = $"駁回應用程式 {clientId}：{request.Reason}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Application rejected successfully" });
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendApp(string id)
    {
        var app = await FindAppByIdOrClientIdAsync(id);
        if (app is null) return NotFound(new { message = "Application not found" });

        var appId = await appManager.GetIdAsync(app);
        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, app);

        descriptor.Properties["status"] = JsonSerializer.SerializeToElement("Suspended");
        await appManager.UpdateAsync(app, descriptor);

        // 緊急強制停用違規 App，後端同步吊銷該 Client 底下的所有流通 Token
        if (!string.IsNullOrEmpty(appId))
        {
            await tokenManager.RevokeAsync(subject: null, client: appId, status: null, type: null);
            await authManager.RevokeAsync(subject: null, client: appId, status: null, type: null);
        }

        var clientId = descriptor.ClientId ?? id;
        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "AppSuspended",
            Actor = User.Identity?.Name ?? "admin",
            Target = clientId,
            Details = $"強制停用違規應用程式 {clientId} 並吊銷所有流通 Token",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Application suspended and all active tokens revoked" });
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreApp(string id)
    {
        var app = await FindAppByIdOrClientIdAsync(id);
        if (app is null) return NotFound(new { message = "Application not found" });

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, app);

        descriptor.Properties["status"] = JsonSerializer.SerializeToElement("Approved");
        await appManager.UpdateAsync(app, descriptor);

        var clientId = descriptor.ClientId ?? id;
        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "AppRestored",
            Actor = User.Identity?.Name ?? "admin",
            Target = clientId,
            Details = $"恢復應用程式 {clientId} 為核准狀態",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Application restored successfully" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateApp([FromBody] CreateAppRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            return BadRequest(new { message = "ClientId is required" });

        var existing = await appManager.FindByClientIdAsync(request.ClientId);
        if (existing is not null)
            return BadRequest(new { message = "Application with this ClientId already exists" });

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = request.ClientId,
            DisplayName = request.DisplayName,
            ClientType = request.ClientType,
            ConsentType = request.ConsentType,
        };

        if (!string.IsNullOrEmpty(request.ClientSecret))
            descriptor.ClientSecret = request.ClientSecret;

        foreach (var uri in request.RedirectUris)
            if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
                descriptor.RedirectUris.Add(parsed);

        foreach (var uri in request.PostLogoutRedirectUris)
            if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
                descriptor.PostLogoutRedirectUris.Add(parsed);

        foreach (var perm in request.Permissions)
            descriptor.Permissions.Add(perm);

        descriptor.Properties["status"] = JsonSerializer.SerializeToElement(request.Status ?? "Sandbox");
        if (!string.IsNullOrEmpty(request.Developer))
            descriptor.Properties["developer"] = JsonSerializer.SerializeToElement(request.Developer);
        if (!string.IsNullOrEmpty(request.RequestReason))
            descriptor.Properties["requestReason"] = JsonSerializer.SerializeToElement(request.RequestReason);

        await appManager.CreateAsync(descriptor);

        return CreatedAtAction(nameof(GetApp), new { id = request.ClientId }, new { message = "Application created successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApp(string id)
    {
        var app = await FindAppByIdOrClientIdAsync(id);
        if (app is null) return NotFound(new { message = "Application not found" });

        await appManager.DeleteAsync(app);
        return Ok(new { message = "Application deleted successfully" });
    }

    private async Task<object?> FindAppByIdOrClientIdAsync(string id)
    {
        var app = await appManager.FindByClientIdAsync(id);
        if (app is not null) return app;
        return await appManager.FindByIdAsync(id);
    }

    private async Task<AppDetailResponse> ToDetailResponseAsync(object app)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, app);

        var id = await appManager.GetIdAsync(app) ?? string.Empty;
        var properties = await appManager.GetPropertiesAsync(app);

        string status = GetStringProperty(properties, "status") ?? "Sandbox";
        string? developer = GetStringProperty(properties, "developer");
        string? rejectReason = GetStringProperty(properties, "rejectReason");
        string? requestReason = GetStringProperty(properties, "requestReason");
        string? submittedAt = GetStringProperty(properties, "submittedAt");

        return new AppDetailResponse
        {
            Id = id,
            ClientId = descriptor.ClientId ?? string.Empty,
            DisplayName = descriptor.DisplayName ?? string.Empty,
            ClientType = descriptor.ClientType ?? string.Empty,
            ConsentType = descriptor.ConsentType ?? string.Empty,
            Developer = developer ?? string.Empty,
            Status = status,
            RejectReason = rejectReason,
            RequestReason = requestReason,
            SubmittedAt = submittedAt,
            RedirectUris = descriptor.RedirectUris.Select(u => u.ToString()).ToList(),
            PostLogoutRedirectUris = descriptor.PostLogoutRedirectUris.Select(u => u.ToString()).ToList(),
            Permissions = descriptor.Permissions.ToList(),
            Requirements = descriptor.Requirements.ToList(),
        };
    }

    private static string? GetStringProperty(ImmutableDictionary<string, JsonElement> properties, string key)
    {
        if (properties.TryGetValue(key, out var element))
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                _ => element.ToString()
            };
        }
        return null;
    }
}
