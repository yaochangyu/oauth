using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OAuth.Client.WebAPI.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        var name = User.FindFirstValue(ClaimTypes.Name)
                   ?? User.FindFirstValue("name");
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email");

        return Ok(new { sub, name, email });
    }

    [HttpGet("protected")]
    public IActionResult Protected()
        => Ok(new { Message = "這是受保護的資源", Time = DateTimeOffset.UtcNow });
}
