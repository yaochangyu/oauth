using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OAuth.Admin.WebAPI.Models;
using OAuth.Admin.WebAPI.Services;
using OpenIddict.Abstractions;

namespace OAuth.Admin.WebAPI.Controllers;

[ApiController]
[Route("api/v1/admin/scopes")]
public class ScopeManageController(
    IOpenIddictScopeManager scopeManager,
    IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScopeDetailResponse>>> GetScopes([FromQuery] string? filter = null)
    {
        var scopes = new List<ScopeDetailResponse>();

        await foreach (var scope in scopeManager.ListAsync())
        {
            var detail = await ToDetailResponseAsync(scope);
            if (!string.IsNullOrEmpty(filter) &&
                !(detail.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                  detail.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                continue;

            scopes.Add(detail);
        }

        return Ok(scopes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScopeDetailResponse>> GetScope(string id)
    {
        var scope = await FindScopeByIdOrNameAsync(id);
        if (scope is null) return NotFound(new { message = "Scope not found" });

        var detail = await ToDetailResponseAsync(scope);
        return Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> CreateScope([FromBody] CreateScopeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Scope name is required" });

        var existing = await scopeManager.FindByNameAsync(request.Name);
        if (existing is not null)
            return BadRequest(new { message = "Scope with this name already exists" });

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
        };

        foreach (var res in request.Resources)
            descriptor.Resources.Add(res);

        descriptor.Properties["isSensitive"] = JsonSerializer.SerializeToElement(request.IsSensitive);

        await scopeManager.CreateAsync(descriptor);

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "ScopeCreated",
            Actor = User.Identity?.Name ?? "admin",
            Target = request.Name,
            Details = $"建立 Scope {request.Name} (敏感權限: {request.IsSensitive})",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Scope created successfully", name = request.Name });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScope(string id, [FromBody] UpdateScopeRequest request)
    {
        var scope = await FindScopeByIdOrNameAsync(id);
        if (scope is null) return NotFound(new { message = "Scope not found" });

        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, scope);

        descriptor.DisplayName = request.DisplayName;
        descriptor.Description = request.Description;
        descriptor.Resources.Clear();
        foreach (var res in request.Resources)
            descriptor.Resources.Add(res);

        descriptor.Properties["isSensitive"] = JsonSerializer.SerializeToElement(request.IsSensitive);

        await scopeManager.UpdateAsync(scope, descriptor);

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "ScopeUpdated",
            Actor = User.Identity?.Name ?? "admin",
            Target = descriptor.Name ?? id,
            Details = $"更新 Scope {descriptor.Name}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Scope updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScope(string id)
    {
        var scope = await FindScopeByIdOrNameAsync(id);
        if (scope is null) return NotFound(new { message = "Scope not found" });

        var name = await scopeManager.GetNameAsync(scope);
        await scopeManager.DeleteAsync(scope);

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "ScopeDeleted",
            Actor = User.Identity?.Name ?? "admin",
            Target = name ?? id,
            Details = $"刪除 Scope {name}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Scope deleted successfully" });
    }

    private async Task<object?> FindScopeByIdOrNameAsync(string id)
    {
        var scope = await scopeManager.FindByNameAsync(id);
        if (scope is not null) return scope;
        return await scopeManager.FindByIdAsync(id);
    }

    private async Task<ScopeDetailResponse> ToDetailResponseAsync(object scope)
    {
        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, scope);

        var id = await scopeManager.GetIdAsync(scope) ?? string.Empty;
        var properties = await scopeManager.GetPropertiesAsync(scope);

        bool isSensitive = false;
        if (properties.TryGetValue("isSensitive", out var element))
        {
            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                isSensitive = element.GetBoolean();
            else if (bool.TryParse(element.GetString(), out var parsed))
                isSensitive = parsed;
        }

        return new ScopeDetailResponse
        {
            Id = id,
            Name = descriptor.Name ?? string.Empty,
            DisplayName = descriptor.DisplayName ?? string.Empty,
            Description = descriptor.Description ?? string.Empty,
            Resources = descriptor.Resources.ToList(),
            IsSensitive = isSensitive,
        };
    }
}
