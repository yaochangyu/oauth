using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Reqnroll;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.Admin.WebAPI.IntegrationTest._01_AppReview;

[Binding]
public class 第三方應用審核流Step : Steps
{
    [Given(@"資料庫已存在待審核第三方應用 ""(.*)"" 其開發者為 ""(.*)"" 狀態為 ""(.*)""")]
    [Given(@"資料庫已存在已核准第三方應用 ""(.*)"" 其開發者為 ""(.*)"" 狀態為 ""(.*)""")]
    public async Task Given資料庫已存在第三方應用(string clientId, string developer, string status)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var existing = await manager.FindByClientIdAsync(clientId);
        if (existing is not null)
        {
            await manager.DeleteAsync(existing);
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = "Secret@123456",
            DisplayName = $"App {clientId}",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Explicit,
            RedirectUris = { new Uri("https://example.com/callback") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + "api",
            },
            Properties =
            {
                ["developer"] = JsonSerializer.SerializeToElement(developer),
                ["status"] = JsonSerializer.SerializeToElement(status),
                ["requestReason"] = JsonSerializer.SerializeToElement("需使用用戶 Email 進行第三方登入"),
                ["submittedAt"] = JsonSerializer.SerializeToElement(DateTime.UtcNow.ToString("o")),
            }
        };

        await manager.CreateAsync(descriptor);
    }

    [Given(@"應用 ""(.*)"" 擁有活躍授權與Token")]
    public async Task Given應用擁有活躍授權與Token(string clientId)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var authManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();

        var app = await appManager.FindByClientIdAsync(clientId);
        app.Should().NotBeNull();
        var appId = await appManager.GetIdAsync(app!);

        var authDescriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = appId,
            Principal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim(Claims.Subject, "test-user-id")], "TestAuth")),
            Subject = "test-user-id",
            Type = AuthorizationTypes.Permanent,
            Status = Statuses.Valid,
            Scopes = { "api", Scopes.Email }
        };

        var authorization = await authManager.CreateAsync(authDescriptor);
        var authId = await authManager.GetIdAsync(authorization);

        var tokenDescriptor = new OpenIddictTokenDescriptor
        {
            ApplicationId = appId,
            AuthorizationId = authId,
            Subject = "test-user-id",
            Type = TokenTypeIdentifiers.AccessToken,
            Status = Statuses.Valid,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(15),
            Payload = "test-access-token-payload",
        };

        await tokenManager.CreateAsync(tokenDescriptor);
    }

    [Then(@"應用 ""(.*)"" 的所有流通Token皆已被吊銷")]
    public async Task Then應用所有流通Token皆已被吊銷(string clientId)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();

        var app = await appManager.FindByClientIdAsync(clientId);
        app.Should().NotBeNull();
        var appId = await appManager.GetIdAsync(app!);

        var activeTokens = 0;
        await foreach (var token in tokenManager.FindByApplicationIdAsync(appId!))
        {
            var status = await tokenManager.GetStatusAsync(token);
            if (status == Statuses.Valid)
            {
                activeTokens++;
            }
        }

        activeTokens.Should().Be(0, "所有流通 Token 應該已被吊銷或刪除");
    }
}
