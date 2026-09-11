using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAuth.AuthServer.DB;
using OAuth.AuthServer.WebAPI.Infrastructure;

namespace OAuth.AuthServer.WebAPI.Account;

[ApiController]
[Route("api/v1/account")]
public class AccountApiController(
    AccountHandler handler,
    IValidator<RegisterRequest> validator,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await handler.RegisterAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Code == "email_already_exists")
                return Conflict(new { result.Error.Code, result.Error.Message });
            return BadRequest(new { result.Error.Code, result.Error.Message });
        }

        return CreatedAtAction(null, new { id = result.Value.UserId }, result.Value);
    }

    /// <summary>
    /// Headless 登入：驗證帳密成功後簽發 HttpOnly Cookie，供 Vue 3 認證站（/login）呼叫。
    /// 掛載 FixedWindowRateLimiter（每 IP 每分鐘最多 5 次），防止字典檔暴力破解。
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "帳號與密碼為必填" });

        // returnUrl 未提供時預設回首頁；有提供則必須通過 Open Redirect 檢查
        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? "/" : request.ReturnUrl;
        if (!ReturnUrlValidator.IsAllowed(Url, returnUrl))
            return BadRequest(new { message = "無效或非法的 returnUrl" });

        // 支援用 email 登入：若輸入值含 @ 則先查 email 取得 username
        var userName = request.UserName;
        if (userName.Contains('@'))
        {
            var userByEmail = await userManager.FindByEmailAsync(userName);
            if (userByEmail?.UserName is not null)
                userName = userByEmail.UserName;
        }

        var result = await signInManager.PasswordSignInAsync(
            userName, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
            return Unauthorized(new { message = "帳號或密碼錯誤" });

        return Ok(new { success = true, returnUrl });
    }

    /// <summary>登出：清除目前的 Identity Cookie 工作階段。</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}

public record LoginRequest(string UserName, string Password, string? ReturnUrl);
