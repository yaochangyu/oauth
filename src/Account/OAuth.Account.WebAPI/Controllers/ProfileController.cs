using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuth.Account.WebAPI.Models;
using OAuth.AuthServer.DB;

namespace OAuth.Account.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/account/profile")]
public class ProfileController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        return Ok(new UserProfileResponse(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            EmailConfirmed: user.EmailConfirmed,
            IsTwoFactorEnabled: isTwoFactorEnabled
        ));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "找不到使用者" });

        user.DisplayName = request.DisplayName;
        user.AvatarUrl = request.AvatarUrl;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "更新個人資料失敗",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        return Ok(new UserProfileResponse(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            EmailConfirmed: user.EmailConfirmed,
            IsTwoFactorEnabled: isTwoFactorEnabled
        ));
    }
}
