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
public class ExternalLoginController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    // 觸發外部 Provider 登入
    [HttpGet("~/connect/authorize/external")]
    public IActionResult Challenge(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(Callback), new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    // 外部 Provider 回調
    [HttpGet("~/connect/authorize/external/callback")]
    public async Task<IActionResult> Callback(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return Redirect($"/Connect/Authorize?error=external_login_failed&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");

        // 嘗試用已綁定的外部登入直接登入
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        ApplicationUser? user;

        if (signInResult.Succeeded)
        {
            user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        }
        else
        {
            // 嘗試用 Email 找到現有帳號並綁定
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                        ?? info.Principal.FindFirstValue(Claims.Email);

            if (email is null)
                return Redirect($"/Connect/Authorize?error=no_email&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");

            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                // 建立新帳號
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name)
                                  ?? info.Principal.FindFirstValue(Claims.Name),
                    AvatarUrl = info.Principal.FindFirstValue("picture")
                                ?? info.Principal.FindFirstValue(Claims.Picture),
                };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return Redirect($"/Connect/Authorize?error=create_user_failed&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");
            }

            // 綁定外部登入
            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: false);
        }

        if (user is null)
            return Redirect($"/Connect/Authorize?error=user_not_found&returnUrl={Uri.EscapeDataString(returnUrl ?? "")}");

        // 若有原始 OpenIddict 授權請求，重導回去完成 Code 發放
        if (!string.IsNullOrEmpty(returnUrl))
            return Redirect(returnUrl);

        return Redirect("/");
    }
}
