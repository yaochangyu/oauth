using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Infrastructure;

/// <summary>
/// 只在 DB 無資料時執行初始化；後續 client 管理請透過 Admin UI。
/// </summary>
public class OpenIddictDataSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("spa-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId    = "spa-client",
                ClientType  = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "SPA Client",
                RedirectUris =
                {
                    new Uri("https://localhost:3000/callback"),
                    new Uri("https://localhost:5173/callback"),
                    new Uri("http://localhost:5173/callback"),
                    new Uri("https://localhost:5200/callback"),
                    new Uri("http://localhost:5200/callback"),
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:3000"),
                    new Uri("https://localhost:5173"),
                    new Uri("http://localhost:5173"),
                    new Uri("https://localhost:5200"),
                    new Uri("http://localhost:5200"),
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            }, cancellationToken);
        }

        if (await manager.FindByClientIdAsync("mvc-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId     = "mvc-client",
                ClientSecret = "mvc-client-secret",
                ClientType   = ClientTypes.Confidential,
                ConsentType  = ConsentTypes.Explicit,
                DisplayName  = "MVC Client",
                RedirectUris           = { new Uri("https://localhost:5101/signin-oidc") },
                PostLogoutRedirectUris = { new Uri("https://localhost:5101/signout-callback-oidc") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            }, cancellationToken);
        }

        if (await manager.FindByClientIdAsync("mvc-implicit", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId     = "mvc-implicit",
                ClientSecret = "mvc-implicit-secret",
                ClientType   = ClientTypes.Confidential,
                ConsentType  = ConsentTypes.Implicit,
                DisplayName  = "MVC Client (Implicit Consent)",
                RedirectUris           = { new Uri("https://localhost:5101/signin-oidc") },
                PostLogoutRedirectUris = { new Uri("https://localhost:5101/signout-callback-oidc") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            }, cancellationToken);
        }

        if (await manager.FindByClientIdAsync("webapi-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId     = "webapi-client",
                ClientSecret = "webapi-client-secret",
                ClientType   = ClientTypes.Confidential,
                DisplayName  = "WebAPI Client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + "api",
                },
            }, cancellationToken);
        }

        if (await manager.FindByClientIdAsync("postman-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId    = "postman-client",
                ClientType  = ClientTypes.Public,
                DisplayName = "Postman",
                RedirectUris =
                {
                    new Uri("https://oauth.pstmn.io/v1/callback"),
                    new Uri("https://localhost"),
                },
                PostLogoutRedirectUris = { new Uri("https://localhost") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            }, cancellationToken);
        }

        if (await manager.FindByClientIdAsync("admin-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId     = "admin-client",
                ClientSecret = "admin-client-secret",
                ClientType   = ClientTypes.Confidential,
                ConsentType  = ConsentTypes.Implicit,
                DisplayName  = "Admin Panel",
                RedirectUris           = { new Uri("https://localhost:7002/signin-oidc") },
                PostLogoutRedirectUris = { new Uri("https://localhost:7002/signout-callback-oidc") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "offline_access",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            }, cancellationToken);
        }

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        if (await scopeManager.FindByNameAsync("api", cancellationToken) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name        = "api",
                DisplayName = "API Access",
                Resources   = { "resource-server" },
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
