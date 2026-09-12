using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace OAuth.Developer.Tests;

public static class TestAssistant
{
    public const string AuthServerIssuer = "https://localhost:7001";
    public const string ValidAudience = "api";
    public const string TestJwtSigningKey = "DeveloperPortalSecretKeyForJwtAuthenticationTest2026!";

    public static readonly RsaSecurityKey AuthServerRsaSigningKey;
    public static readonly RsaSecurityKey AttackerRsaSigningKey;

    static TestAssistant()
    {
        var rsaAuth = RSA.Create(2048);
        AuthServerRsaSigningKey = new RsaSecurityKey(rsaAuth)
        {
            KeyId = "authserver-test-rsa-signing-key"
        };

        var rsaAttacker = RSA.Create(2048);
        AttackerRsaSigningKey = new RsaSecurityKey(rsaAttacker)
        {
            KeyId = "attacker-untrusted-rsa-key"
        };
    }

    public static async Task<PostgreSqlContainer> CreatePostgresContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("oauth_developer_test")
            .WithUsername("oauth")
            .WithPassword("oauth_pass")
            .Build();

        await container.StartAsync();
        return container;
    }

    public static void SetDbConnectionEnvironmentVariable(string connectionString)
        => Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);

    public static string GenerateValidAuthServerToken(string userId, string role = "Developer")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = AuthServerIssuer,
            Audience = ValidAudience,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"DevUser-{userId}"),
                new Claim(ClaimTypes.Email, $"{userId}@example.com"),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(AuthServerRsaSigningKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateTestJwtToken(string userId, string role = "Developer")
        => GenerateValidAuthServerToken(userId, role);

    public static string GenerateForgedSymmetricToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtSigningKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = AuthServerIssuer,
            Audience = ValidAudience,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"DevUser-{userId}"),
                new Claim(ClaimTypes.Email, $"{userId}@example.com"),
                new Claim(ClaimTypes.Role, "Developer"),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateForgedRsaToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = AuthServerIssuer,
            Audience = ValidAudience,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Developer"),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(AttackerRsaSigningKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateForgedIssuerToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://attacker-fake-authserver.com",
            Audience = ValidAudience,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(AuthServerRsaSigningKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateForgedAudienceToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = AuthServerIssuer,
            Audience = "unauthorized_fake_api",
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(AuthServerRsaSigningKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
