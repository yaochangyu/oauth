using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Testcontainers.PostgreSql;

namespace OAuth.AuthServer.IntegrationTest;

public static class TestAssistant
{
    public const string AuthServerIssuer = "https://localhost:7001";
    public const string ValidAudience = "api";

    public static readonly RsaSecurityKey AuthServerRsaSigningKey;

    static TestAssistant()
    {
        var rsaAuth = RSA.Create(2048);
        AuthServerRsaSigningKey = new RsaSecurityKey(rsaAuth)
        {
            KeyId = "authserver-test-rsa-signing-key"
        };
    }

    public static async Task<PostgreSqlContainer> CreatePostgresContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("oauth_test")
            .WithUsername("oauth")
            .WithPassword("oauth_pass")
            .Build();

        await container.StartAsync();
        return container;
    }

    public static void SetDbConnectionEnvironmentVariable(string connectionString)
        => Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);

    public static string GenerateTestJwtToken(string userId, string role = "Developer")
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
                new Claim(ClaimTypes.Name, $"User-{userId}"),
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
}
