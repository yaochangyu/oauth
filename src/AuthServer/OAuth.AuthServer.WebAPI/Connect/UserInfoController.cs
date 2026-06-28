using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Connect;

[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
public class UserInfoController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        var userId = User.GetClaim(Claims.Subject);
        if (userId is null)
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var claims = new Dictionary<string, object>
        {
            [Claims.Subject] = user.Id,
            [Claims.Email] = user.Email ?? string.Empty,
            [Claims.Name] = user.DisplayName ?? user.Email ?? string.Empty,
        };

        if (user.AvatarUrl is not null)
            claims[Claims.Picture] = user.AvatarUrl;

        return Ok(claims);
    }
}
