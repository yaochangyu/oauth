using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using System.Collections.Concurrent;

namespace OAuth.Developer.WebAPI.Controllers;

[ApiController]
[Route("api/v1/developer/account")]
public class DeveloperAccountController : ControllerBase
{
    // In-memory / cache store for developer profiles (can be augmented by DB user claims)
    private static readonly ConcurrentDictionary<string, DeveloperStatusResponse> DeveloperProfiles = new();

    [HttpPost("enable")]
    public IActionResult EnableDeveloper([FromBody] EnableDeveloperRequest request)
    {
        var userId = GetDeveloperUserId();

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
        if (DeveloperProfiles.TryGetValue(userId, out var profile))
        {
            return Ok(profile);
        }

        return Ok(new DeveloperStatusResponse
        {
            IsDeveloperEnabled = true, // Default enabled in dev / registered users
            OrganizationName = "Default Developer Organization",
            ContactEmail = "developer@example.com",
            RegisteredAt = DateTimeOffset.UtcNow,
        });
    }

    [HttpPut("profile")]
    public IActionResult UpdateProfile([FromBody] EnableDeveloperRequest request)
    {
        var userId = GetDeveloperUserId();
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

    private string GetDeveloperUserId()
    {
        if (Request.Headers.TryGetValue("X-Developer-UserId", out var headerUserId) && !string.IsNullOrWhiteSpace(headerUserId))
            return headerUserId.ToString();

        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return sub ?? "default_developer_user";
    }
}
