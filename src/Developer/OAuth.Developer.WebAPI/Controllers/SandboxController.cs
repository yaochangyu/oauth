using Microsoft.AspNetCore.Mvc;
using OAuth.Developer.WebAPI.Models;
using OAuth.Developer.WebAPI.Services;
using System.Text;
using System.Web;

namespace OAuth.Developer.WebAPI.Controllers;

[ApiController]
[Route("api/v1/developer/sandbox")]
public class SandboxController(
    DeveloperApplicationService developerApplicationService,
    SecretRotationManager secretRotationManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("generate-authorize-url")]
    public IActionResult GenerateAuthorizeUrl([FromBody] GenerateAuthorizeUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.RedirectUri))
            return BadRequest(new { error = "ClientId 與 RedirectUri 為必填欄位" });

        var authServerBaseUrl = configuration["AuthServer:Authority"] ?? "https://localhost:7001";
        var uriBuilder = new StringBuilder($"{authServerBaseUrl.TrimEnd('/')}/connect/authorize?");

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["response_type"] = "code";
        queryParams["client_id"] = request.ClientId;
        queryParams["redirect_uri"] = request.RedirectUri;
        queryParams["scope"] = string.IsNullOrWhiteSpace(request.Scope) ? "openid profile email" : request.Scope;
        
        if (!string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            queryParams["code_challenge"] = request.CodeChallenge;
            queryParams["code_challenge_method"] = string.IsNullOrWhiteSpace(request.CodeChallengeMethod) ? "S256" : request.CodeChallengeMethod;
        }

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            queryParams["state"] = request.State;
        }

        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            queryParams["prompt"] = request.Prompt;
        }

        uriBuilder.Append(queryParams.ToString());

        return Ok(new GenerateAuthorizeUrlResponse
        {
            AuthorizeUrl = uriBuilder.ToString(),
        });
    }

    [HttpPost("validate-credentials")]
    public async Task<IActionResult> ValidateCredentials([FromBody] ValidateCredentialsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return BadRequest(new ValidateCredentialsResponse
            {
                IsValid = false,
                Error = "invalid_client",
                ErrorDescription = "缺少 ClientId 或 ClientSecret",
            });
        }

        var app = await developerApplicationService.FindAppByIdOrClientIdAsync(request.ClientId, cancellationToken);
        if (app == null)
        {
            return BadRequest(new ValidateCredentialsResponse
            {
                IsValid = false,
                Error = "invalid_client",
                ErrorDescription = "找不到對應之 ClientId",
            });
        }

        var isValid = await secretRotationManager.ValidateSecretAsync(app, request.ClientSecret, cancellationToken);
        if (!isValid)
        {
            return BadRequest(new ValidateCredentialsResponse
            {
                IsValid = false,
                Error = "invalid_client",
                ErrorDescription = "ClientSecret 驗證失敗（已作廢、過期或金鑰不相符）",
            });
        }

        return Ok(new ValidateCredentialsResponse
        {
            IsValid = true,
        });
    }
}
