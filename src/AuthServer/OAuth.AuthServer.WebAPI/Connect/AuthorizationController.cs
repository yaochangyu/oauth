using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace OAuth.AuthServer.WebAPI.Connect;

[ApiController]
public class AuthorizationController : ControllerBase
{
    // OpenIddict passthrough：收到 /connect/authorize 請求後，重導至登入頁
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public IActionResult Authorize()
    {
        _ = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("找不到 OpenIddict 授權請求");

        var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
        return Redirect($"/Connect/Authorize?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
