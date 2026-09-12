using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuth.Account.WebAPI.Models;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.Account.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/account/consents")]
public class ConsentsController(
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictTokenManager tokenManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuthorizedApps()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var authorizations = new List<object>();
        await foreach (var auth in authorizationManager.FindAsync(
            subject: userId,
            client: null,
            status: Statuses.Valid,
            type: null,
            scopes: null,
            cancellationToken: HttpContext.RequestAborted))
        {
            authorizations.Add(auth);
        }

        var result = new List<AuthorizedAppDto>();
        foreach (var auth in authorizations)
        {
            var authId = await authorizationManager.GetIdAsync(auth);
            var appId = await authorizationManager.GetApplicationIdAsync(auth);
            var app = !string.IsNullOrEmpty(appId) ? await applicationManager.FindByIdAsync(appId) : null;
            var scopes = await authorizationManager.GetScopesAsync(auth);
            var createdAt = await authorizationManager.GetCreationDateAsync(auth);

            var clientId = app is not null ? await applicationManager.GetClientIdAsync(app) : "未知 Client";
            var clientDisplayName = app is not null ? (await applicationManager.GetDisplayNameAsync(app) ?? clientId) : "未知應用";

            result.Add(new AuthorizedAppDto(
                AuthorizationId: authId ?? string.Empty,
                ClientId: clientId ?? string.Empty,
                ClientDisplayName: clientDisplayName ?? "未知應用",
                Scopes: scopes.ToList(),
                AuthorizedAt: createdAt?.UtcDateTime ?? DateTime.UtcNow
            ));
        }

        return Ok(result);
    }

    [HttpDelete("{authorizationId}")]
    public async Task<IActionResult> RevokeAppConsent(string authorizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var auth = await authorizationManager.FindByIdAsync(authorizationId);
        if (auth is null)
            return NotFound(new { message = "找不到該授權紀錄" });

        var authSubject = await authorizationManager.GetSubjectAsync(auth);
        if (authSubject != userId)
            return Forbid();

        // 1. 原生撤銷授權
        await authorizationManager.TryRevokeAsync(auth, HttpContext.RequestAborted);

        // 2. 級聯作廢該授權底下的所有 Access Token 與 Refresh Token
        await foreach (var token in tokenManager.FindByAuthorizationIdAsync(authorizationId))
        {
            await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);
        }

        return NoContent();
    }
}
