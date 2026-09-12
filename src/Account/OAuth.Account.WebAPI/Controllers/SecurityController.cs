using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.Account.WebAPI.Models;
using OAuth.AuthServer.DB;

namespace OAuth.Account.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/account/security")]
public class SecurityController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("2fa-status")]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        var hasAuthenticator = !string.IsNullOrEmpty(key);

        return Ok(new TwoFactorStatusResponse(
            IsTwoFactorEnabled: isTwoFactorEnabled,
            HasAuthenticator: hasAuthenticator
        ));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "新密碼與確認密碼不相符" });
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "密碼修改失敗",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(new { message = "密碼修改成功" });
    }

    [HttpPost("2fa/generate-key")]
    public async Task<IActionResult> GenerateTwoFactorKey()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await userManager.GetEmailAsync(user) ?? user.UserName ?? "user";
        var authenticatorUri = $"otpauth://totp/OAuthPortal:{Uri.EscapeDataString(email)}?secret={key}&issuer=OAuthPortal&digits=6";

        return Ok(new Generate2FaKeyResponse(
            SharedKey: key ?? string.Empty,
            AuthenticatorUri: authenticatorUri
        ));
    }

    [HttpPost("2fa/verify-and-enable")]
    public async Task<IActionResult> VerifyAndEnableTwoFactor([FromBody] Verify2FaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        var verificationCode = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!isValid)
        {
            return BadRequest(new { message = "驗證碼無效或已過期" });
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);

        return Ok(new { message = "雙層驗證 (2FA) 已成功啟用" });
    }

    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] Disable2FaRequest? request = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);

        return Ok(new { message = "雙層驗證 (2FA) 已成功停用" });
    }
}
