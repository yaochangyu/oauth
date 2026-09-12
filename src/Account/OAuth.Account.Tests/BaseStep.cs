using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Json.Path;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;
using Reqnroll;
using System.Collections.Immutable;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testcontainers.PostgreSql;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OAuth.Account.Tests;

[Binding]
[CollectionDefinition("OAuth.Account.Tests", DisableParallelization = true)]
public class BaseStep : Steps
{
    private static readonly List<IContainer> TestContainers = [];
    public static AccountTestFactory? Factory { get; private set; }

    private const string 字串等於 = "字串等於";
    private const string 數值等於 = "數值等於";
    private const string 布林值等於 = "布林值等於";
    private const string 包含字串 = "包含字串";
    private const string 不為空 = "不為空";

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        var postgres = await TestAssistant.CreatePostgresContainerAsync();
        TestContainers.Add(postgres);
        TestAssistant.SetDbConnectionEnvironmentVariable(postgres.GetConnectionString());

        Factory = new AccountTestFactory();
        await Factory.InitializeDatabaseAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        Factory?.Dispose();
        for (var i = TestContainers.Count - 1; i >= 0; i--)
            await TestContainers[i].StopAsync();
        TestContainers.Clear();
    }

    [Given(@"初始化測試伺服器")]
    public void Given初始化測試伺服器()
    {
        var client = Factory!.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost/"),
        });
        this.ScenarioContext["HttpClient"] = client;
    }

    [Given(@"資料庫中存在使用者 ""(.*)"" 且密碼為 ""(.*)"" 暱稱為 ""(.*)""")]
    public async Task Given資料庫中存在使用者(string userId, string password, string displayName)
    {
        using var scope = Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId}@example.com",
                Email = $"{userId}@example.com",
                DisplayName = displayName,
                EmailConfirmed = true,
            };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }
        else
        {
            user.DisplayName = displayName;
            user.Email = $"{userId}@example.com";
            await userManager.UpdateAsync(user);
        }
    }

    [Given(@"調用端已使用使用者身分 ""(.*)"" 取得有效 JWT Token")]
    [When(@"調用端已使用使用者身分 ""(.*)"" 取得有效 JWT Token")]
    public void Given調用端已使用使用者身分取得有效JWTToken(string userId)
    {
        var token = TestAssistant.GenerateValidAuthServerToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentUserId"] = userId;
    }

    [Given(@"調用端未帶入任何認證 Token")]
    [When(@"調用端未帶入任何認證 Token")]
    public void Given調用端未帶入任何認證Token()
    {
        if (this.ScenarioContext.ContainsKey("Headers"))
        {
            var headers = (Dictionary<string, string>)this.ScenarioContext["Headers"];
            headers.Remove("Authorization");
            this.ScenarioContext["Headers"] = headers;
        }
    }

    [Given(@"調用端使用無效的 JWT Token")]
    [When(@"調用端使用無效的 JWT Token")]
    public void Given調用端使用無效的JWTToken()
    {
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = "Bearer invalid_token_xyz";
        this.ScenarioContext["Headers"] = headers;
    }

    [Given(@"調用端使用自製對稱金鑰簽發偽造 JWT Token 冒充 ""(.*)""")]
    [When(@"調用端使用自製對稱金鑰簽發偽造 JWT Token 冒充 ""(.*)""")]
    public void Given調用端使用自製對稱金鑰簽發偽造JWTToken(string userId)
    {
        var token = TestAssistant.GenerateForgedSymmetricToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentUserId"] = userId;
    }

    [Given(@"調用端使用未知 RSA 私鑰簽發偽造 JWT Token 冒充 ""(.*)""")]
    [When(@"調用端使用未知 RSA 私鑰簽發偽造 JWT Token 冒充 ""(.*)""")]
    public void Given調用端使用未知RSA私鑰簽發偽造JWTToken(string userId)
    {
        var token = TestAssistant.GenerateForgedRsaToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentUserId"] = userId;
    }

    [Given(@"調用端使用偽造 Issuer 的 JWT Token 冒充 ""(.*)""")]
    [When(@"調用端使用偽造 Issuer 的 JWT Token 冒充 ""(.*)""")]
    public void Given調用端使用偽造Issuer的JWTToken(string userId)
    {
        var token = TestAssistant.GenerateForgedIssuerToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentUserId"] = userId;
    }

    [Given(@"調用端使用偽造 Audience 的 JWT Token 冒充 ""(.*)""")]
    [When(@"調用端使用偽造 Audience 的 JWT Token 冒充 ""(.*)""")]
    public void Given調用端使用偽造Audience的JWTToken(string userId)
    {
        var token = TestAssistant.GenerateForgedAudienceToken(userId);
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        headers["Authorization"] = $"Bearer {token}";
        this.ScenarioContext["Headers"] = headers;
        this.ScenarioContext["CurrentUserId"] = userId;
    }

    [Given(@"調用端已準備 Header 參數")]
    public void Given調用端已準備Header參數(Table table)
    {
        var headers = this.ScenarioContext.ContainsKey("Headers")
            ? (Dictionary<string, string>)this.ScenarioContext["Headers"]
            : new Dictionary<string, string>();

        foreach (var row in table.Rows)
            foreach (var header in table.Header)
                headers[header] = row[header];

        this.ScenarioContext["Headers"] = headers;
    }

    [Given(@"調用端已準備 Body 參數\(Json\)")]
    public void Given調用端已準備BodyJson(string json)
    {
        json = ReplacePlaceholders(json);
        this.ScenarioContext["Body"] = json;
    }

    [When(@"調用端發送 ""(.*)"" 請求至 ""(.*)""")]
    public async Task When調用端發送請求至(string method, string url)
    {
        url = ReplacePlaceholders(url);

        var client = (HttpClient)this.ScenarioContext["HttpClient"];
        var httpMethod = new HttpMethod(method);
        using var request = new HttpRequestMessage(httpMethod, url);

        if (this.ScenarioContext.ContainsKey("Headers"))
        {
            var headers = (Dictionary<string, string>)this.ScenarioContext["Headers"];
            foreach (var h in headers)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        if (this.ScenarioContext.ContainsKey("Body"))
        {
            var body = (string)this.ScenarioContext["Body"];
            request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        this.ScenarioContext["Response"] = response;
        this.ScenarioContext["ResponseBody"] = responseBody;

        // Clear one-time request body
        this.ScenarioContext.Remove("Body");
    }

    [Then(@"調用端應收到 HTTP 狀態碼為 ""(.*)""")]
    public void Then調用端應收到HTTP狀態碼為(int expectedStatusCode)
    {
        var response = (HttpResponseMessage)this.ScenarioContext["Response"];
        var responseBody = (string)this.ScenarioContext["ResponseBody"];
        ((int)response.StatusCode).Should().Be(expectedStatusCode, $"Response body was: {responseBody}");
    }

    [Then(@"回應內容驗證")]
    public void Then回應內容驗證(Table table)
    {
        var responseBody = (string)this.ScenarioContext["ResponseBody"];
        var jsonNode = JsonNode.Parse(responseBody);
        jsonNode.Should().NotBeNull();

        foreach (var row in table.Rows)
        {
            var jsonPath = row["欄位路徑"];
            var op = row["驗證方式"];
            var expected = row["預期值"];

            var path = JsonPath.Parse(jsonPath);
            var result = path.Evaluate(jsonNode);
            result.Matches.Should().NotBeEmpty($"路徑 '{jsonPath}' 未找到相符節點，原始 JSON: {responseBody}");

            var actualNode = result.Matches[0].Value;

            switch (op)
            {
                case 字串等於:
                    actualNode?.GetValue<string>().Should().Be(expected);
                    break;
                case 數值等於:
                    actualNode?.GetValue<long>().Should().Be(long.Parse(expected));
                    break;
                case 布林值等於:
                    actualNode?.GetValue<bool>().Should().Be(bool.Parse(expected));
                    break;
                case 包含字串:
                    actualNode?.ToString().Should().Contain(expected);
                    break;
                case 不為空:
                    actualNode?.ToString().Should().NotBeNullOrWhiteSpace();
                    break;
                default:
                    throw new NotSupportedException($"不支援的驗證方式：{op}");
            }
        }
    }

    [Given(@"從回應中儲存變數 ""(.*)"" 為 JSON 欄位 ""(.*)""")]
    [When(@"從回應中儲存變數 ""(.*)"" 為 JSON 欄位 ""(.*)""")]
    [Then(@"從回應中儲存變數 ""(.*)"" 為 JSON 欄位 ""(.*)""")]
    public void When從回應中儲存變數為JSON欄位(string variableName, string jsonPath)
    {
        var responseBody = (string)this.ScenarioContext["ResponseBody"];
        var jsonNode = JsonNode.Parse(responseBody);
        jsonNode.Should().NotBeNull();

        var path = JsonPath.Parse(jsonPath);
        var result = path.Evaluate(jsonNode);
        result.Matches.Should().NotBeEmpty($"路徑 '{jsonPath}' 未找到相符節點");

        var node = result.Matches[0].Value;
        string? val = null;
        if (node is JsonValue jsonVal)
        {
            if (jsonVal.TryGetValue<string>(out var strVal))
                val = strVal;
            else
                val = jsonVal.ToString();
        }
        else
        {
            val = node?.ToString();
        }
        this.ScenarioContext[variableName] = val;
    }

    [Given(@"系統中存在第三方應用程式 ClientId ""(.*)"" 顯示名稱 ""(.*)""")]
    public async Task Given系統中存在第三方應用程式(string clientId, string displayName)
    {
        using var scope = Factory!.Services.CreateScope();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var existing = await appManager.FindByClientIdAsync(clientId);
        if (existing is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                DisplayName = displayName,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Explicit,
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Email,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                }
            };
            descriptor.RedirectUris.Add(new Uri("https://oauth.pstmn.io/v1/callback"));
            await appManager.CreateAsync(descriptor);
        }
    }

    [Given(@"使用者 ""(.*)"" 已授權第三方應用程式 ""(.*)"" 擁有 Scopes ""(.*)"" 並取得授權識別碼儲存至 ""(.*)""")]
    public async Task Given使用者已授權第三方應用程式(string userId, string clientId, string scopesStr, string varName)
    {
        using var scope = Factory!.Services.CreateScope();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var authManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();

        var app = await appManager.FindByClientIdAsync(clientId)
            ?? throw new InvalidOperationException($"找不到應用程式 {clientId}");
        var appId = await appManager.GetIdAsync(app);

        var scopes = scopesStr.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToImmutableHashSet();

        var descriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = appId,
            Subject = userId,
            Type = AuthorizationTypes.Permanent,
            Status = Statuses.Valid,
        };
        foreach (var s in scopes)
            descriptor.Scopes.Add(s);

        var auth = await authManager.CreateAsync(descriptor);
        var authId = await authManager.GetIdAsync(auth);

        this.ScenarioContext[varName] = authId;
    }

    [Given(@"為授權 ""(.*)"" 簽發 Refresh Token 儲存至 ""(.*)""")]
    public async Task Given為授權簽發RefreshToken(string authVarName, string refreshTokenVar)
    {
        var authId = this.ScenarioContext.ContainsKey(authVarName)
            ? (string)this.ScenarioContext[authVarName]
            : authVarName;

        using var scope = Factory!.Services.CreateScope();
        var authManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();

        var auth = await authManager.FindByIdAsync(authId)
            ?? throw new InvalidOperationException($"找不到授權 {authId}");

        var appId = await authManager.GetApplicationIdAsync(auth);
        var subject = await authManager.GetSubjectAsync(auth);

        var tokenValue = Guid.NewGuid().ToString("N") + "-refresh-token-test";

        var descriptor = new OpenIddictTokenDescriptor
        {
            ApplicationId = appId,
            AuthorizationId = authId,
            Subject = subject,
            Type = TokenTypeIdentifiers.RefreshToken,
            Status = Statuses.Valid,
            Payload = tokenValue,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
        };

        var token = await tokenManager.CreateAsync(descriptor);
        var tokenId = await tokenManager.GetIdAsync(token);

        this.ScenarioContext[refreshTokenVar] = tokenId;
        this.ScenarioContext[$"{refreshTokenVar}_Payload"] = tokenValue;
        this.ScenarioContext[$"{refreshTokenVar}_AuthId"] = authId;
    }

    [When(@"使用 Refresh Token ""(.*)"" 透過 OpenIddict 換發 Token")]
    public async Task When使用RefreshToken透過OpenIddict換發Token(string refreshTokenVar)
    {
        var tokenId = this.ScenarioContext.ContainsKey(refreshTokenVar)
            ? (string)this.ScenarioContext[refreshTokenVar]
            : refreshTokenVar;

        using var scope = Factory!.Services.CreateScope();
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var authManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();

        var token = await tokenManager.FindByIdAsync(tokenId);
        if (token is null)
        {
            this.ScenarioContext["ExchangeResult"] = "invalid_grant";
            this.ScenarioContext["ExchangeError"] = "Token not found";
            return;
        }

        var tokenStatus = await tokenManager.GetStatusAsync(token);
        if (tokenStatus != Statuses.Valid)
        {
            this.ScenarioContext["ExchangeResult"] = "invalid_grant";
            this.ScenarioContext["ExchangeError"] = $"Token status is {tokenStatus}";
            return;
        }

        var authId = await tokenManager.GetAuthorizationIdAsync(token);
        if (!string.IsNullOrEmpty(authId))
        {
            var auth = await authManager.FindByIdAsync(authId);
            if (auth is null || await authManager.GetStatusAsync(auth) != Statuses.Valid)
            {
                this.ScenarioContext["ExchangeResult"] = "invalid_grant";
                this.ScenarioContext["ExchangeError"] = "Authorization revoked or invalid";
                return;
            }
        }

        this.ScenarioContext["ExchangeResult"] = "success";
        this.ScenarioContext["NewAccessToken"] = "new_access_token_sample";
    }

    [Then(@"換發結果應失敗並帶有錯誤碼 ""(.*)""")]
    public void Then換發結果應失敗並帶有錯誤碼(string expectedError)
    {
        var result = (string)this.ScenarioContext["ExchangeResult"];
        result.Should().Be(expectedError);
    }

    [Then(@"換發結果應成功並取得新的 Access Token")]
    public void Then換發結果應成功並取得新的AccessToken()
    {
        var result = (string)this.ScenarioContext["ExchangeResult"];
        result.Should().Be("success");
    }

    [When(@"產生 Authenticator 2FA 驗證碼並填入 Body 欄位 ""(.*)""")]
    public async Task When產生Authenticator2FA驗證碼並填入Body欄位(string fieldName)
    {
        var userId = (string)this.ScenarioContext["CurrentUserId"];
        using var scope = Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"找不到使用者 {userId}");

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        key.Should().NotBeNullOrWhiteSpace($"User {userId} should have an authenticator key generated");

        var code = TestAssistant.GenerateTotpCode(key!);

        var bodyObj = new JsonObject
        {
            [fieldName] = code
        };

        this.ScenarioContext["Body"] = bodyObj.ToJsonString();
    }

    [Then(@"驗證資料庫中使用者 ""(.*)"" 的雙層驗證狀態為 ""(.*)""")]
    public async Task Then驗證資料庫中使用者的雙層驗證狀態(string userId, string expectedStatus)
    {
        using var scope = Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();

        var isEnabled = await userManager.GetTwoFactorEnabledAsync(user!);
        isEnabled.Should().Be(bool.Parse(expectedStatus));
    }

    [Then(@"驗證資料庫中使用者 ""(.*)"" 的暱稱為 ""(.*)""")]
    public async Task Then驗證資料庫中使用者的暱稱(string userId, string expectedDisplayName)
    {
        using var scope = Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();
        user!.DisplayName.Should().Be(expectedDisplayName);
    }

    private string ReplacePlaceholders(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        foreach (var key in this.ScenarioContext.Keys)
        {
            var val = this.ScenarioContext[key]?.ToString();
            if (val != null)
            {
                input = input.Replace($"{{{{{key}}}}}", val);
                if (key.StartsWith("Saved"))
                {
                    var shortKey = key["Saved".Length..];
                    input = input.Replace($"{{{{{shortKey}}}}}", val);
                }
            }
        }
        return input;
    }
}
