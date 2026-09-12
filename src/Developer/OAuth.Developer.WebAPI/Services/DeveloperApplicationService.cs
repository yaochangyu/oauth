using Microsoft.EntityFrameworkCore;
using OAuth.AuthServer.DB;
using OAuth.Developer.WebAPI.Models;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using System.Security.Cryptography;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.Developer.WebAPI.Services;

public class DeveloperApplicationService(
    IOpenIddictApplicationManager applicationManager,
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<AppResponse> CreateAppAsync(
        string developerUserId,
        CreateAppRequest request,
        CancellationToken cancellationToken = default)
    {
        var appType = request.AppType?.Trim() ?? DeveloperAppConstants.TypeWeb;
        var isConfidential = string.Equals(appType, DeveloperAppConstants.TypeWeb, StringComparison.OrdinalIgnoreCase);

        var clientId = $"app_{GenerateRandomId(12)}";
        string? plaintextSecret = null;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = request.DisplayName,
            ClientType = isConfidential ? ClientTypes.Confidential : ClientTypes.Public,
            ConsentType = ConsentTypes.Explicit,
            ApplicationType = isConfidential ? ApplicationTypes.Web : ApplicationTypes.Native,
        };

        if (isConfidential)
        {
            plaintextSecret = SecretRotationManager.GenerateSecret();
            descriptor.ClientSecret = plaintextSecret;
        }

        // 全類型強制 PKCE 與授權碼流程 (RFC 9700)
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange); // 強制 PKCE

        if (isConfidential)
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
        }

        // Scopes
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + "offline_access");

        if (request.RequestedScopes != null)
        {
            foreach (var scope in request.RequestedScopes)
            {
                if (!string.IsNullOrWhiteSpace(scope))
                    descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope.Trim());
            }
        }

        // Redirect URIs
        foreach (var uri in request.RedirectUris)
        {
            if (!string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri.Trim(), UriKind.Absolute, out var validUri))
                descriptor.RedirectUris.Add(validUri);
        }

        // Post Logout Redirect URIs
        foreach (var uri in request.PostLogoutRedirectUris)
        {
            if (!string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri.Trim(), UriKind.Absolute, out var validUri))
                descriptor.PostLogoutRedirectUris.Add(validUri);
        }

        var now = DateTimeOffset.UtcNow;
        descriptor.Properties[DeveloperAppConstants.PropDeveloperUserId] = JsonSerializer.SerializeToElement(developerUserId);
        descriptor.Properties[DeveloperAppConstants.PropAppType] = JsonSerializer.SerializeToElement(appType);
        descriptor.Properties[DeveloperAppConstants.PropDescription] = JsonSerializer.SerializeToElement(request.Description ?? string.Empty);
        descriptor.Properties[DeveloperAppConstants.PropLogoUrl] = JsonSerializer.SerializeToElement(request.LogoUrl ?? string.Empty);
        descriptor.Properties[DeveloperAppConstants.PropStatus] = JsonSerializer.SerializeToElement(DeveloperAppConstants.StatusSandbox);
        descriptor.Properties[DeveloperAppConstants.PropCreatedAt] = JsonSerializer.SerializeToElement(now.ToString("O"));

        var created = (OpenIddictEntityFrameworkCoreApplication)await applicationManager.CreateAsync(descriptor, cancellationToken);

        var response = await MapToResponseAsync(created, cancellationToken);
        response.ClientSecret = plaintextSecret;
        return response;
    }

    public async Task<List<AppResponse>> GetDeveloperAppsAsync(
        string developerUserId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var apps = await db.OpenIddictApplications.ToListAsync(cancellationToken);

        var result = new List<AppResponse>();
        foreach (var app in apps)
        {
            var props = await SecretRotationManager.GetPropertiesDictionaryAsync(app, cancellationToken);
            if (props.TryGetValue(DeveloperAppConstants.PropDeveloperUserId, out var devIdElement))
            {
                var owner = devIdElement.GetString();
                if (string.Equals(owner, developerUserId, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(await MapToResponseAsync(app, cancellationToken));
                }
            }
        }

        return result;
    }

    public async Task<OpenIddictEntityFrameworkCoreApplication?> FindAppByIdOrClientIdAsync(
        string idOrClientId,
        CancellationToken cancellationToken = default)
    {
        var app = (OpenIddictEntityFrameworkCoreApplication?)await applicationManager.FindByIdAsync(idOrClientId, cancellationToken);
        if (app != null)
            return app;

        return (OpenIddictEntityFrameworkCoreApplication?)await applicationManager.FindByClientIdAsync(idOrClientId, cancellationToken);
    }

    public async Task<AppResponse?> GetAppDetailsAsync(
        string idOrClientId,
        CancellationToken cancellationToken = default)
    {
        var app = await FindAppByIdOrClientIdAsync(idOrClientId, cancellationToken);
        if (app == null)
            return null;

        return await MapToResponseAsync(app, cancellationToken);
    }

    public async Task<AppResponse?> UpdateAppAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        UpdateAppRequest request,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            descriptor.DisplayName = request.DisplayName;

        var properties = await SecretRotationManager.GetPropertiesDictionaryAsync(app, cancellationToken);

        if (request.Description != null)
            properties[DeveloperAppConstants.PropDescription] = JsonSerializer.SerializeToElement(request.Description);

        if (request.LogoUrl != null)
            properties[DeveloperAppConstants.PropLogoUrl] = JsonSerializer.SerializeToElement(request.LogoUrl);

        descriptor.Properties.Clear();
        foreach (var kv in properties)
            descriptor.Properties[kv.Key] = kv.Value;

        if (request.RedirectUris != null)
        {
            descriptor.RedirectUris.Clear();
            foreach (var uri in request.RedirectUris)
            {
                if (!string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri.Trim(), UriKind.Absolute, out var validUri))
                    descriptor.RedirectUris.Add(validUri);
            }
        }

        if (request.PostLogoutRedirectUris != null)
        {
            descriptor.PostLogoutRedirectUris.Clear();
            foreach (var uri in request.PostLogoutRedirectUris)
            {
                if (!string.IsNullOrWhiteSpace(uri) && Uri.TryCreate(uri.Trim(), UriKind.Absolute, out var validUri))
                    descriptor.PostLogoutRedirectUris.Add(validUri);
            }
        }

        if (request.RequestedScopes != null)
        {
            // Remove old custom scopes, retain standard endpoints and grant types
            var preservedPermissions = descriptor.Permissions
                .Where(p => !p.StartsWith(Permissions.Prefixes.Scope))
                .ToList();

            descriptor.Permissions.Clear();
            foreach (var p in preservedPermissions)
                descriptor.Permissions.Add(p);

            // Re-add scopes
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "offline_access");

            foreach (var scope in request.RequestedScopes)
            {
                if (!string.IsNullOrWhiteSpace(scope))
                    descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope.Trim());
            }
        }

        await applicationManager.UpdateAsync(app, descriptor, cancellationToken);
        return await MapToResponseAsync(app, cancellationToken);
    }

    public async Task<bool> DeleteAppAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        CancellationToken cancellationToken = default)
    {
        await applicationManager.DeleteAsync(app, cancellationToken);
        return true;
    }

    public async Task<ReviewStatusResponse> SubmitReviewAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, cancellationToken);

        var properties = await SecretRotationManager.GetPropertiesDictionaryAsync(app, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        properties[DeveloperAppConstants.PropStatus] = JsonSerializer.SerializeToElement(DeveloperAppConstants.StatusInReview);
        properties[DeveloperAppConstants.PropReviewSubmittedAt] = JsonSerializer.SerializeToElement(now.ToString("O"));
        if (!string.IsNullOrWhiteSpace(notes))
            properties[DeveloperAppConstants.PropReviewNotes] = JsonSerializer.SerializeToElement(notes);

        descriptor.Properties.Clear();
        foreach (var kv in properties)
            descriptor.Properties[kv.Key] = kv.Value;

        await applicationManager.UpdateAsync(app, descriptor, cancellationToken);

        return new ReviewStatusResponse
        {
            AppId = app.Id ?? string.Empty,
            Status = DeveloperAppConstants.StatusInReview,
            SubmittedAt = now,
            ReviewNotes = notes,
        };
    }

    public async Task<AppResponse> MapToResponseAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, cancellationToken);

        var properties = await SecretRotationManager.GetPropertiesDictionaryAsync(app, cancellationToken);

        var appType = properties.TryGetValue(DeveloperAppConstants.PropAppType, out var at) ? at.GetString() ?? DeveloperAppConstants.TypeWeb : DeveloperAppConstants.TypeWeb;
        var description = properties.TryGetValue(DeveloperAppConstants.PropDescription, out var desc) ? desc.GetString() : null;
        var logoUrl = properties.TryGetValue(DeveloperAppConstants.PropLogoUrl, out var logo) ? logo.GetString() : null;
        var status = properties.TryGetValue(DeveloperAppConstants.PropStatus, out var st) ? st.GetString() ?? DeveloperAppConstants.StatusSandbox : DeveloperAppConstants.StatusSandbox;

        DateTimeOffset? createdAt = null;
        if (properties.TryGetValue(DeveloperAppConstants.PropCreatedAt, out var ca) && DateTimeOffset.TryParse(ca.GetString(), out var cat))
            createdAt = cat;

        DateTimeOffset? reviewSubmittedAt = null;
        if (properties.TryGetValue(DeveloperAppConstants.PropReviewSubmittedAt, out var rsa) && DateTimeOffset.TryParse(rsa.GetString(), out var rsat))
            reviewSubmittedAt = rsat;

        var scopes = descriptor.Permissions
            .Where(p => p.StartsWith(Permissions.Prefixes.Scope))
            .Select(p => p[Permissions.Prefixes.Scope.Length..])
            .Distinct()
            .ToList();

        var requiresPkce = descriptor.Requirements.Contains(Requirements.Features.ProofKeyForCodeExchange);

        return new AppResponse
        {
            Id = app.Id ?? string.Empty,
            ClientId = app.ClientId ?? string.Empty,
            DisplayName = app.DisplayName ?? string.Empty,
            AppType = appType,
            ClientType = app.ClientType ?? (string.Equals(appType, DeveloperAppConstants.TypeWeb, StringComparison.OrdinalIgnoreCase) ? ClientTypes.Confidential : ClientTypes.Public),
            Description = description,
            LogoUrl = logoUrl,
            Status = status,
            RequiresPkce = requiresPkce,
            RedirectUris = descriptor.RedirectUris.Select(u => u.ToString()).ToList(),
            PostLogoutRedirectUris = descriptor.PostLogoutRedirectUris.Select(u => u.ToString()).ToList(),
            RequestedScopes = scopes,
            CreatedAt = createdAt,
            ReviewSubmittedAt = reviewSubmittedAt,
        };
    }

    private static string GenerateRandomId(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant()[..length];
    }
}
