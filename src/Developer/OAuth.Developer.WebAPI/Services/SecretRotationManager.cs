using Microsoft.AspNetCore.Identity;
using OAuth.Developer.WebAPI.Models;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace OAuth.Developer.WebAPI.Services;

public class SecretRotationManager(
    IOpenIddictApplicationManager applicationManager)
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public static string GenerateSecret()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var randomString = Convert.ToHexString(randomBytes).ToLowerInvariant();
        return $"sk_live_{randomString}";
    }

    public async Task<(string NewSecret, DateTimeOffset RetiringSecretExpiresAt)> RotateSecretAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, cancellationToken);

        var currentSecretHash = app.ClientSecret;
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Update properties with retiring secret info
        var properties = await GetPropertiesDictionaryAsync(app, cancellationToken);
        if (!string.IsNullOrEmpty(currentSecretHash))
        {
            properties[DeveloperAppConstants.PropRetiringSecretHash] = JsonSerializer.SerializeToElement(currentSecretHash);
            properties[DeveloperAppConstants.PropRetiringSecretExpiresAt] = JsonSerializer.SerializeToElement(expiresAt.ToString("O"));
        }

        descriptor.Properties.Clear();
        foreach (var kv in properties)
        {
            descriptor.Properties[kv.Key] = kv.Value;
        }

        var newSecret = GenerateSecret();
        descriptor.ClientSecret = newSecret;

        await applicationManager.UpdateAsync(app, descriptor, cancellationToken);

        return (newSecret, expiresAt);
    }

    public async Task RevokeRetiringSecretAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app, cancellationToken);

        var properties = await GetPropertiesDictionaryAsync(app, cancellationToken);
        properties.Remove(DeveloperAppConstants.PropRetiringSecretHash);
        properties.Remove(DeveloperAppConstants.PropRetiringSecretExpiresAt);

        descriptor.Properties.Clear();
        foreach (var kv in properties)
        {
            descriptor.Properties[kv.Key] = kv.Value;
        }

        await applicationManager.UpdateAsync(app, descriptor, cancellationToken);
    }

    public async Task<bool> ValidateSecretAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        string secret,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secret))
            return false;

        // 1. Verify against active secret
        if (await applicationManager.ValidateClientSecretAsync(app, secret, cancellationToken))
        {
            return true;
        }

        // 2. Verify against retiring secret if within 7-day grace period
        var properties = await GetPropertiesDictionaryAsync(app, cancellationToken);
        if (properties.TryGetValue(DeveloperAppConstants.PropRetiringSecretHash, out var hashElement) &&
            properties.TryGetValue(DeveloperAppConstants.PropRetiringSecretExpiresAt, out var expiresElement))
        {
            var hash = hashElement.GetString();
            var expiresAtStr = expiresElement.GetString();

            if (!string.IsNullOrEmpty(hash) &&
                DateTimeOffset.TryParse(expiresAtStr, out var expiresAt) &&
                DateTimeOffset.UtcNow <= expiresAt)
            {
                // Validate secret against the stored retiring hash
                if (ValidatePasswordHash(hash, secret))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<CredentialsResponse> GetCredentialsInfoAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        CancellationToken cancellationToken = default)
    {
        var properties = await GetPropertiesDictionaryAsync(app, cancellationToken);
        var hasActiveSecret = !string.IsNullOrEmpty(app.ClientSecret);
        var hasRetiring = false;
        DateTimeOffset? retiringExpiresAt = null;
        double? expiresInSeconds = null;

        if (properties.TryGetValue(DeveloperAppConstants.PropRetiringSecretExpiresAt, out var expiresElement) &&
            properties.ContainsKey(DeveloperAppConstants.PropRetiringSecretHash))
        {
            var expiresStr = expiresElement.GetString();
            if (DateTimeOffset.TryParse(expiresStr, out var exp))
            {
                if (DateTimeOffset.UtcNow <= exp)
                {
                    hasRetiring = true;
                    retiringExpiresAt = exp;
                    expiresInSeconds = Math.Max(0, (exp - DateTimeOffset.UtcNow).TotalSeconds);
                }
            }
        }

        return new CredentialsResponse
        {
            ClientId = app.ClientId ?? string.Empty,
            ClientType = app.ClientType ?? string.Empty,
            HasActiveSecret = hasActiveSecret,
            ActiveSecretMasked = hasActiveSecret ? "sk_live_••••••••••••••••" : null,
            HasRetiringSecret = hasRetiring,
            RetiringSecretExpiresAt = retiringExpiresAt,
            RetiringSecretExpiresInSeconds = expiresInSeconds,
        };
    }

    private bool ValidatePasswordHash(string hash, string secret)
    {
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(new object(), hash, secret);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                return true;
        }
        catch
        {
            // fallback plain comparison or alternative
        }

        return false;
    }

    public static async Task<Dictionary<string, JsonElement>> GetPropertiesDictionaryAsync(
        OpenIddictEntityFrameworkCoreApplication app,
        CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(app.Properties))
            return dict;

        try
        {
            using var doc = JsonDocument.Parse(app.Properties);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }
        }
        catch
        {
            // ignore malformed properties
        }

        return await Task.FromResult(dict);
    }
}
