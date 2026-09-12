using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.Admin.WebAPI.Models;
using OAuth.Admin.WebAPI.Services;
using OAuth.AuthServer.DB;

namespace OAuth.Admin.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,admin")]
[Route("api/v1/admin/users")]
public class UserManageController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryResponse>>> GetUsers(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(filter)) ||
                (u.Email != null && u.Email.Contains(filter)));
        }

        var users = query.OrderBy(u => u.UserName).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var result = new List<UserSummaryResponse>();

        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new UserSummaryResponse
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                LockoutEnd = u.LockoutEnd,
                Roles = roles.ToList(),
            });
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailResponse>> GetUser(string id)
    {
        var user = await FindUserByIdOrNameAsync(id);
        if (user is null) return NotFound(new { message = "User not found" });

        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);

        return Ok(new UserDetailResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            Roles = roles.ToList(),
            Claims = claims.Select(c => new UserClaimModel { Type = c.Type, Value = c.Value }).ToList(),
        });
    }

    [HttpPut("{id}/lockout")]
    public async Task<IActionResult> LockoutUser(string id)
    {
        var user = await FindUserByIdOrNameAsync(id);
        if (user is null) return NotFound(new { message = "User not found" });

        await userManager.SetLockoutEnabledAsync(user, true);
        var lockoutEndDate = DateTimeOffset.UtcNow.AddYears(100);
        await userManager.SetLockoutEndDateAsync(user, lockoutEndDate);

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "UserLockedOut",
            Actor = User.Identity?.Name ?? "admin",
            Target = user.UserName ?? user.Id,
            Details = $"凍結違規使用者帳號 {user.UserName}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "User locked out successfully", lockoutEnd = lockoutEndDate });
    }

    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await FindUserByIdOrNameAsync(id);
        if (user is null) return NotFound(new { message = "User not found" });

        await userManager.SetLockoutEndDateAsync(user, null);

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "UserUnlocked",
            Actor = User.Identity?.Name ?? "admin",
            Target = user.UserName ?? user.Id,
            Details = $"解除使用者帳號 {user.UserName} 凍結",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "User unlocked successfully" });
    }

    [HttpPost("{id}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessions(string id)
    {
        var user = await FindUserByIdOrNameAsync(id);
        if (user is null) return NotFound(new { message = "User not found" });

        await userManager.UpdateSecurityStampAsync(user);

        await auditLogService.RecordAsync(new AuditLogEntry
        {
            EventType = "UserSessionsRevoked",
            Actor = User.Identity?.Name ?? "admin",
            Target = user.UserName ?? user.Id,
            Details = $"強制登出使用者 {user.UserName} 的所有已連線工作階段",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
        });

        return Ok(new { message = "User sessions revoked successfully" });
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AddRole(string id, [FromBody] AddRoleRequest request)
    {
        var user = await FindUserByIdOrNameAsync(id);
        if (user is null) return NotFound(new { message = "User not found" });

        if (!await roleManager.RoleExistsAsync(request.RoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(request.RoleName));
        }

        var result = await userManager.AddToRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = $"Role '{request.RoleName}' added to user" });
    }

    [HttpDelete("{id}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(string id, string roleName)
    {
        var user = await FindUserByIdOrNameAsync(id);
        if (user is null) return NotFound(new { message = "User not found" });

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = $"Role '{roleName}' removed from user" });
    }

    private async Task<ApplicationUser?> FindUserByIdOrNameAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is not null) return user;
        return await userManager.FindByNameAsync(id);
    }
}
