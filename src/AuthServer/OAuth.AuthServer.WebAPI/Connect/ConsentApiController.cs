using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using OAuth.AuthServer.WebAPI.Infrastructure;
using OpenIddict.Abstractions;

namespace OAuth.AuthServer.WebAPI.Connect;

/// <summary>
/// Headless 授權同意 API，供 Vue 3 認證站（/consent）呼叫。
/// 短期同意憑證使用 IDistributedCache（開發環境 InMemory，可平滑切換 Redis 供多節點叢集使用）。
/// </summary>
[ApiController]
[Route("api/v1/connect")]
[Authorize]
public class ConsentApiController(
    IOpenIddictApplicationManager appManager,
    IDistributedCache cache) : ControllerBase
{
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
    };

    [HttpGet("consent-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConsentInfo([FromQuery] string returnUrl)
    {
        // 嚴格防範 Open Redirect
        if (!ReturnUrlValidator.IsAllowed(Url, returnUrl))
            return BadRequest(new { message = "無效或非法的 returnUrl" });

        var query = QueryHelpers.ParseQuery(new Uri("https://localhost" + returnUrl).Query);
        var clientId = query.TryGetValue("client_id", out var cid) ? cid.ToString() : string.Empty;
        var scope = query.TryGetValue("scope", out var s) ? s.ToString() : string.Empty;

        var app = await appManager.FindByClientIdAsync(clientId);
        if (app is null)
            return BadRequest(new { message = "找不到對應的第三方應用程式" });

        return Ok(new
        {
            clientId,
            clientDisplayName = await appManager.GetDisplayNameAsync(app) ?? clientId,
            scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            returnUrl,
        });
    }

    [HttpPost("consent-accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Accept([FromBody] ConsentDecisionRequest request)
        => WriteDecisionAsync(request, "granted");

    [HttpPost("consent-deny")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Deny([FromBody] ConsentDecisionRequest request)
        => WriteDecisionAsync(request, "denied");

    private async Task<IActionResult> WriteDecisionAsync(ConsentDecisionRequest request, string decision)
    {
        if (!ReturnUrlValidator.IsAllowed(Url, request.ReturnUrl))
            return BadRequest(new { message = "非法跳轉目標" });

        var token = Guid.NewGuid().ToString("N");
        await cache.SetStringAsync($"consent:{token}", $"{decision}:{request.ClientId}", CacheEntryOptions);

        var separator = request.ReturnUrl.Contains('?') ? "&" : "?";
        return Ok(new { redirectUrl = $"{request.ReturnUrl}{separator}__ct={token}" });
    }
}

public record ConsentDecisionRequest(string ReturnUrl, string ClientId);
