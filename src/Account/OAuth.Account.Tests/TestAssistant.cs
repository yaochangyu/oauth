using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace OAuth.Account.Tests;

public static class TestAssistant
{
    public const string AuthServerIssuer = "https://localhost:7001";
    public const string ValidAudience = "api";
    public const string TestJwtSigningKey = "AccountPortalSecretKeyForJwtAuthenticationTest2026!";

    public static readonly RsaSecurityKey AuthServerRsaSigningKey;
    public static readonly RsaSecurityKey AttackerRsaSigningKey;

    static TestAssistant()
    {
        var rsaAuth = RSA.Create(2048);
        AuthServerRsaSigningKey = new RsaSecurityKey(rsaAuth)
        {
            KeyId = "authserver-account-test-rsa-signing-key"
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
            .WithDatabase("oauth_account_test")
            .WithUsername("oauth")
            .WithPassword("oauth_pass")
            .Build();

        await container.StartAsync();
        return container;
    }

    public static void SetDbConnectionEnvironmentVariable(string connectionString)
        => Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);

    public static string GenerateValidAuthServerToken(string userId, string? email = null, string? name = null)
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
                new Claim(ClaimTypes.Name, name ?? $"User-{userId}"),
                new Claim(ClaimTypes.Email, email ?? $"{userId}@example.com"),
                new Claim("scope", "openid profile email offline_access api"),
            ]),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(AuthServerRsaSigningKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

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
                new Claim(ClaimTypes.Name, $"FakeUser-{userId}"),
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

    public static string GenerateTotpCode(string base32Key)
    {
        var keyBytes = Base32Decode(base32Key);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = unixTimestamp / 30;

        var timestepBytes = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestepBytes);
        }

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(timestepBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7f) << 24)
                   | ((hash[offset + 1] & 0xff) << 16)
                   | ((hash[offset + 2] & 0xff) << 8)
                   | (hash[offset + 3] & 0xff);

        var otp = binary % 1000000;
        return otp.ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.Trim().TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();

        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in input)
        {
            var val = Base32Chars.IndexOf(c);
            if (val < 0) continue;

            buffer = (buffer << 5) | val;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}
