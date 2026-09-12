using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.Admin.WebAPI.Models;
using OAuth.Admin.WebAPI.Services;

namespace OAuth.Admin.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,admin")]
[Route("api/v1/admin/roles")]
public class RoleManageController(
    RoleManager<IdentityRole> roleManager,
    IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<RoleSummaryResponse>> GetRoles(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = roleManager.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(r => r.Name != null && r.Name.Contains(filter));
        }

        var roles = query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleSummaryResponse
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty
            })
            .ToList();

        return Ok(roles);
    }

    [HttpGet("{nameOrId}")]
    public async Task<ActionResult<RoleSummaryResponse>> GetRole(string nameOrId)
    {
        var role = await FindRoleByNameOrIdAsync(nameOrId);
        if (role is null) return NotFound(new { message = "Role not found" });

        return Ok(new RoleSummaryResponse
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Role name is required" });

        var roleName = request.Name.Trim();
        if (await roleManager.RoleExistsAsync(roleName))
            return BadRequest(new { message = $"Role '{roleName}' already exists" });

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "RoleCreated",
            Actor = User.Identity?.Name ?? "admin",
            Target = roleName,
            Details = $"建立角色 {roleName}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Role created successfully", name = roleName });
    }

    [HttpDelete("{nameOrId}")]
    public async Task<IActionResult> DeleteRole(string nameOrId)
    {
        var role = await FindRoleByNameOrIdAsync(nameOrId);
        if (role is null) return NotFound(new { message = "Role not found" });

        var roleName = role.Name ?? nameOrId;
        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "RoleDeleted",
            Actor = User.Identity?.Name ?? "admin",
            Target = roleName,
            Details = $"刪除角色 {roleName}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "Role deleted successfully" });
    }

    private async Task<IdentityRole?> FindRoleByNameOrIdAsync(string nameOrId)
    {
        var role = await roleManager.FindByNameAsync(nameOrId);
        if (role is not null) return role;
        return await roleManager.FindByIdAsync(nameOrId);
    }
}
