using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace OAuth.Developer.Tests;

public static class TestAssistant
{
    public const string TestJwtSigningKey = "DeveloperPortalSecretKeyForJwtAuthenticationTest2026!";

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

    public static string GenerateTestJwtToken(string userId, string role = "Developer")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtSigningKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"DevUser-{userId}"),
                new Claim(ClaimTypes.Email, $"{userId}@example.com"),
                new Claim(ClaimTypes.Role, role),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
