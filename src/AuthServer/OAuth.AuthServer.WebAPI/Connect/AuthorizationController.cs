using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Connect;

[ApiController]
public class AuthorizationController(
    UserManager<ApplicationUser> userManager) : ControllerBase
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
            return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var user = await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("找不到使用者");

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
