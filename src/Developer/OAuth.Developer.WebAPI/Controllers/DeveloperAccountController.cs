using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace OAuth.Developer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/developer/account")]
public class DeveloperAccountController : ControllerBase
{
    // In-memory / cache store for developer profiles (can be augmented by DB user claims / DeveloperDbContext)
    private static readonly ConcurrentDictionary<string, DeveloperStatusResponse> DeveloperProfiles = new();

    [HttpPost("enable")]
    public IActionResult EnableDeveloper([FromBody] EnableDeveloperRequest request)
    {
        var userId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var profile = new DeveloperStatusResponse
        {
            IsDeveloperEnabled = true,
            OrganizationName = request.OrganizationName ?? "Individual Developer",
            ContactEmail = request.ContactEmail,
            RegisteredAt = DateTimeOffset.UtcNow,
        };

        DeveloperProfiles[userId] = profile;
        return Ok(profile);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var userId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        if (DeveloperProfiles.TryGetValue(userId, out var profile))
        {
            return Ok(profile);
        }

        return Ok(new DeveloperStatusResponse
        {
            IsDeveloperEnabled = true,
            OrganizationName = "Default Developer Organization",
            ContactEmail = "developer@example.com",
            RegisteredAt = DateTimeOffset.UtcNow,
        });
    }

    [HttpPut("profile")]
    public IActionResult UpdateProfile([FromBody] EnableDeveloperRequest request)
    {
        var userId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var profile = new DeveloperStatusResponse
        {
            IsDeveloperEnabled = true,
            OrganizationName = request.OrganizationName,
            ContactEmail = request.ContactEmail,
            RegisteredAt = DateTimeOffset.UtcNow,
        };
        DeveloperProfiles[userId] = profile;
        return Ok(profile);
    }

    private string? GetDeveloperUserId()
    {
        return User.FindFirst("sub")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
