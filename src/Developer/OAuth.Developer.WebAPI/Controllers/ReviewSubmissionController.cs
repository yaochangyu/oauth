using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using OAuth.Developer.WebAPI.Services;
using System.Security.Claims;

namespace OAuth.Developer.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/developer/apps/{id}")]
public class ReviewSubmissionController(
    DeveloperApplicationService developerApplicationService) : ControllerBase
{
    [HttpPost("submit-review")]
    public async Task<IActionResult> SubmitReview(string id, [FromBody] SubmitReviewRequest? request, CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        if (!await developerApplicationService.IsAppOwnedByDeveloperAsync(app, developerUserId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "無權提交此應用程式審核" });

        var result = await developerApplicationService.SubmitReviewAsync(app, request?.Notes, cancellationToken);
        return Ok(result);
    }

    [HttpGet("review-status")]
    public async Task<IActionResult> GetReviewStatus(string id, CancellationToken cancellationToken)
    {
        var developerUserId = GetDeveloperUserId();
        if (string.IsNullOrEmpty(developerUserId))
            return Unauthorized(new { error = "無法識別使用者身分" });

        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        if (!await developerApplicationService.IsAppOwnedByDeveloperAsync(app, developerUserId, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "無權查詢此應用程式審核狀態" });

        var details = await developerApplicationService.GetAppDetailsAsync(id, cancellationToken);
        return Ok(new ReviewStatusResponse
        {
            AppId = details!.Id,
            Status = details.Status,
            SubmittedAt = details.ReviewSubmittedAt,
            ReviewNotes = null,
        });
    }

    private string? GetDeveloperUserId()
    {
        return User.FindFirst("sub")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
