using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using OAuth.Developer.WebAPI.Services;

namespace OAuth.Developer.WebAPI.Controllers;

[ApiController]
[Route("api/v1/developer/apps/{id}")]
public class CredentialsController(
    DeveloperApplicationService developerApplicationService,
    SecretRotationManager secretRotationManager) : ControllerBase
{
    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentials(string id, CancellationToken cancellationToken)
    {
        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        var creds = await secretRotationManager.GetCredentialsInfoAsync(app, cancellationToken);
        return Ok(creds);
    }

    [HttpPost("rotate-secret")]
    public async Task<IActionResult> RotateSecret(string id, CancellationToken cancellationToken)
    {
        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        var (newSecret, retiringExpiresAt) = await secretRotationManager.RotateSecretAsync(app, cancellationToken);

        return Ok(new RotateSecretResponse
        {
            ClientId = app.ClientId ?? string.Empty,
            NewSecret = newSecret,
            RetiringSecretExpiresAt = retiringExpiresAt,
            Message = "金鑰輪替成功。新金鑰已生效；舊金鑰已轉入 7 天相容過渡期，支援雙金鑰並存零停機換密。",
        });
    }

    [HttpPost("revoke-retiring-secret")]
    public async Task<IActionResult> RevokeRetiringSecret(string id, CancellationToken cancellationToken)
    {
        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(id, cancellationToken);
        if (app == null)
            return NotFound(new { error = "應用程式不存在" });

        await secretRotationManager.RevokeRetiringSecretAsync(app, cancellationToken);

        return Ok(new RevokeRetiringSecretResponse
        {
            Success = true,
            Message = "舊金鑰已成功立即作廢，目前僅新金鑰具備驗證效力。",
        });
    }
}
