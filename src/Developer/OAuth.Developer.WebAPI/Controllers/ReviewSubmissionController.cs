using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using OAuth.Developer.WebAPI.Services;

namespace OAuth.Developer.WebAPI.Controllers;

[ApiController]
[Route("api/v1/developer/apps/{id}")]
public class ReviewSubmissionController(
    DeveloperApplicationService developerApplicationService) : ControllerBase
{
    [HttpPost("submit-review")]
    public async Task<IActionResult> SubmitReview(string id, [FromBody] SubmitReviewRequest? request, CancellationToken cancellationToken)
    {
        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        var result = await developerApplicationService.SubmitReviewAsync(app, request?.Notes, cancellationToken);
        return Ok(result);
    }

    [HttpGet("review-status")]
    public async Task<IActionResult> GetReviewStatus(string id, CancellationToken cancellationToken)
    {
        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        var details = await developerApplicationService.GetAppDetailsAsync(id, cancellationToken);
        return Ok(new ReviewStatusResponse
        {
            AppId = details!.Id,
            Status = details.Status,
            SubmittedAt = details.ReviewSubmittedAt,
            ReviewNotes = null,
        });
    }
}
