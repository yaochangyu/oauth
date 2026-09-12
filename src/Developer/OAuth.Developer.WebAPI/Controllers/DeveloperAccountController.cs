using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OAuth.Developer.WebAPI.Data;
using OAuth.Developer.WebAPI.Models;
using System.Security.Claims;

namespace OAuth.Developer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/developer/account")]
public class DeveloperAccountController(DeveloperDbContext dbContext) : ControllerBase
{
    [HttpPost("enable")]
    public async Task<IActionResult> EnableDeveloper([FromBody] EnableDeveloperRequest request, CancellationToken cancellationToken)
    {
        var userId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var profile = await dbContext.DeveloperProfiles.FindAsync([userId], cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (profile == null)
        {
            profile = new DeveloperProfile
            {
                UserId = userId,
                IsDeveloperEnabled = true,
                OrganizationName = request.OrganizationName ?? "Individual Developer",
                ContactEmail = request.ContactEmail,
                RegisteredAt = now,
            };
            dbContext.DeveloperProfiles.Add(profile);
        }
        else
        {
            profile.IsDeveloperEnabled = true;
            if (!string.IsNullOrWhiteSpace(request.OrganizationName))
                profile.OrganizationName = request.OrganizationName;
            if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                profile.ContactEmail = request.ContactEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new DeveloperStatusResponse
        {
            IsDeveloperEnabled = profile.IsDeveloperEnabled,
            OrganizationName = profile.OrganizationName,
            ContactEmail = profile.ContactEmail,
            RegisteredAt = profile.RegisteredAt,
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var profile = await dbContext.DeveloperProfiles.FindAsync([userId], cancellationToken);
        if (profile != null)
        {
            return Ok(new DeveloperStatusResponse
            {
                IsDeveloperEnabled = profile.IsDeveloperEnabled,
                OrganizationName = profile.OrganizationName,
                ContactEmail = profile.ContactEmail,
                RegisteredAt = profile.RegisteredAt,
            });
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
    public async Task<IActionResult> UpdateProfile([FromBody] EnableDeveloperRequest request, CancellationToken cancellationToken)
    {
        var userId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var profile = await dbContext.DeveloperProfiles.FindAsync([userId], cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (profile == null)
        {
            profile = new DeveloperProfile
            {
                UserId = userId,
                IsDeveloperEnabled = true,
                OrganizationName = request.OrganizationName,
                ContactEmail = request.ContactEmail,
                RegisteredAt = now,
            };
            dbContext.DeveloperProfiles.Add(profile);
        }
        else
        {
            profile.IsDeveloperEnabled = true;
            profile.OrganizationName = request.OrganizationName;
            profile.ContactEmail = request.ContactEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new DeveloperStatusResponse
        {
            IsDeveloperEnabled = profile.IsDeveloperEnabled,
            OrganizationName = profile.OrganizationName,
            ContactEmail = profile.ContactEmail,
            RegisteredAt = profile.RegisteredAt,
        });
    }

    private string? GetDeveloperUserId()
    {
        return User.FindFirst("sub")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
