using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Connect;

[ApiController]
public class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IDistributedCache cache) : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("找不到 OpenIddict 授權請求");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
            return Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var user = await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("找不到使用者");

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("找不到呼叫端應用程式");
        var consentType = await applicationManager.GetConsentTypeAsync(application);
        var appId = await applicationManager.GetIdAsync(application);

        // 查詢是否已存在有效的永久授權紀錄 (Permanent Authorization)
        var authorizations = new List<object>();
        await foreach (var auth in authorizationManager.FindAsync(
            subject: user.Id,
            client: appId!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()))
        {
            authorizations.Add(auth);
        }
        var authorization = authorizations.LastOrDefault();

        var consentToken = Request.Query["__ct"].ToString();
        var consentDecision = string.Empty;
        var consentClientId = string.Empty;

        if (!string.IsNullOrEmpty(consentToken))
        {
            var tokenValue = await cache.GetStringAsync($"consent:{consentToken}");
            if (tokenValue is not null)
            {
                await cache.RemoveAsync($"consent:{consentToken}");
                var parts = tokenValue.Split(':', 2);
                consentDecision = parts[0];
                consentClientId = parts.Length > 1 ? parts[1] : string.Empty;
            }

            if (consentDecision == "denied" && consentClientId == request.ClientId)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization was denied by the user.",
                    }));
            }

            if (consentDecision != "granted" || consentClientId != request.ClientId)
            {
                // 去除舊的 __ct，避免 returnUrl 帶過期 token
                var cleanQs = QueryString.Create(
                    Request.Query
                        .Where(kvp => kvp.Key != "__ct")
                        .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value.ToString())));
                var returnUrl = Request.Path + cleanQs;
                return Redirect($"/consent?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
        }
        else
        {
            // 無 consentToken 時：若無既有永久授權且 Client 為 Explicit Consent，需導向同意頁面
            if (authorization is null && (string.IsNullOrEmpty(consentType) || consentType == ConsentTypes.Explicit))
            {
                var returnUrl = Request.Path + Request.QueryString;
                return Redirect($"/consent?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
        }

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);

        var subClaim = new Claim(Claims.Subject, user.Id);
        subClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        identity.AddClaim(subClaim);

        var emailClaim = new Claim(Claims.Email, user.Email!);
        if (request.HasScope(Scopes.Email))
            emailClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        else
            emailClaim.SetDestinations(Destinations.AccessToken);
        identity.AddClaim(emailClaim);

        var nameClaim = new Claim(Claims.Name, user.DisplayName ?? user.Email!);
        if (request.HasScope(Scopes.Profile))
            nameClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        else
            nameClaim.SetDestinations(Destinations.AccessToken);
        identity.AddClaim(nameClaim);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        // 若尚無永久授權，建立新的永久授權紀錄
        if (authorization is null)
        {
            authorization = await authorizationManager.CreateAsync(
                principal: principal,
                subject: user.Id,
                client: appId!,
                type: AuthorizationTypes.Permanent,
                scopes: principal.GetScopes());
        }

        var authId = await authorizationManager.GetIdAsync(authorization);
        identity.SetAuthorizationId(authId);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/endsession")]
    [HttpPost("~/connect/endsession")]
    public async Task<IActionResult> EndSession()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }
}
