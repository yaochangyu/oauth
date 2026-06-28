using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Infrastructure;

public class OpenIddictDataSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // SPA Client（Vue/React）— public client，Authorization Code + PKCE
        if (await manager.FindByClientIdAsync("spa-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "spa-client",
                ClientType = ClientTypes.Public,
                DisplayName = "SPA Client",
                RedirectUris =
                {
                    new Uri("https://localhost:3000/callback"),
                    new Uri("https://localhost:5173/callback"),
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:3000"),
                    new Uri("https://localhost:5173"),
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
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange,
                },
            }, cancellationToken);
        }

        // MVC Client（ASP.NET Core 10 MVC）— confidential client，Cookie SSO
        if (await manager.FindByClientIdAsync("mvc-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "mvc-client",
                ClientSecret = "mvc-client-secret",
                ClientType = ClientTypes.Confidential,
                DisplayName = "MVC Client",
                RedirectUris =
                {
                    new Uri("https://localhost:5101/signin-oidc"),
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:5101/signout-callback-oidc"),
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
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange,
                },
            }, cancellationToken);
        }

        // WebAPI Client — confidential client，Client Credentials
        if (await manager.FindByClientIdAsync("webapi-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "webapi-client",
                ClientSecret = "webapi-client-secret",
                ClientType = ClientTypes.Confidential,
                DisplayName = "WebAPI Client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + "api",
                },
            }, cancellationToken);
        }

        // Postman Client — 測試用，public client
        if (await manager.FindByClientIdAsync("postman-client", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "postman-client",
                ClientType = ClientTypes.Public,
                DisplayName = "Postman",
                RedirectUris =
                {
                    new Uri("https://oauth.pstmn.io/v1/callback"),
                    new Uri("https://localhost"),
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
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange,
                },
            }, cancellationToken);
        }

        // Scope: api
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        if (await scopeManager.FindByNameAsync("api", cancellationToken) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api",
                DisplayName = "API Access",
                Resources = { "resource-server" },
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
