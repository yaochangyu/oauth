using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace OAuth.Client.Mvc.Controllers;

[Authorize]
public class ProfileController : Controller
{
    public async Task<IActionResult> Index()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

        ViewBag.AccessToken = accessToken;
        ViewBag.HasRefreshToken = !string.IsNullOrEmpty(refreshToken);
        ViewBag.Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return View();
    }
}
