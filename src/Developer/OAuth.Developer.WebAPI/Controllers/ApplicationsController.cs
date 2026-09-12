using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using OAuth.Developer.WebAPI.Services;
using System.Security.Claims;

namespace OAuth.Developer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/developer/apps")]
public class ApplicationsController(
    DeveloperApplicationService developerApplicationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetApps(CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var apps = await developerApplicationService.GetDeveloperAppsAsync(developerUserId, cancellationToken);
        return Ok(apps);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApp(string id, CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        if (!await developerApplicationService.IsAppOwnedByDeveloperAsync(app, developerUserId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "無權存取此應用程式資源" });

        var details = await developerApplicationService.GetAppDetailsAsync(id, cancellationToken);
        return Ok(details);
    }

    [HttpPost]
    public async Task<IActionResult> CreateApp([FromBody] CreateAppRequest request, CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest(new { error = "DisplayName 為必填欄位" });

        var created = await developerApplicationService.CreateAppAsync(developerUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetApp), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApp(string id, [FromBody] UpdateAppRequest request, CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        if (!await developerApplicationService.IsAppOwnedByDeveloperAsync(app, developerUserId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "無權修改此應用程式資源" });

        var updated = await developerApplicationService.UpdateAppAsync(app, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApp(string id, CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        if (!await developerApplicationService.IsAppOwnedByDeveloperAsync(app, developerUserId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "無權刪除此應用程式資源" });

        await developerApplicationService.DeleteAppAsync(app, cancellationToken);
        return NoContent();
    }

    private string? GetDeveloperUserId()
    {
        return User.FindFirst("sub")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
