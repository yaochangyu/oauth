using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Account;

[ApiController]
[Route("api/v1/account")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class AccountManagementController(
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    /// 目前使用者資訊：同時支援 Identity Cookie（Vue 3 /login 建立的瀏覽器工作階段，
    /// 由 Headless 登入流程呼叫）與 OpenIddict Bearer Token（一般資源伺服器呼叫方）。
    /// 兩者共用同一路由，避免與此 controller 既有 API 產生 Ambiguous Route。
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Identity.Application," + OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetClaim(Claims.Subject) ?? userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
        });
    }

    [HttpGet("external-logins")]
    public async Task<IActionResult> GetExternalLogins()
    {
        var userId = User.GetClaim(Claims.Subject);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var logins = await userManager.GetLoginsAsync(user);
        return Ok(logins.Select(l => new { l.LoginProvider, l.ProviderDisplayName }));
    }

    [HttpDelete("external-logins/{provider}")]
    public async Task<IActionResult> RemoveExternalLogin(string provider)
    {
        var userId = User.GetClaim(Claims.Subject);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var logins = await userManager.GetLoginsAsync(user);
        var login = logins.FirstOrDefault(l => l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (login is null) return NotFound(new { Message = $"找不到 {provider} 的綁定" });

        // 至少保留一種登入方式
        var hasPassword = await userManager.HasPasswordAsync(user);
        if (!hasPassword && logins.Count == 1)
            return BadRequest(new { Message = "必須保留至少一種登入方式" });

        var result = await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => new { e.Code, e.Description }));

        return NoContent();
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.GetClaim(Claims.Subject);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => new { e.Code, e.Description }));

        return NoContent();
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
