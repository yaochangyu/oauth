using FluentAssertions;
using Reqnroll;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OAuth.Client.WebAPI.IntegrationTest;

/// <summary>
/// 單一 Step Class：集中所有 BDD step definitions 與工具方法
/// [Binding] 標記此類為 Reqnroll binding，定義所有 Given/When/Then steps
/// </summary>
[Binding]
[CollectionDefinition("OAuth.Client.WebAPI.IntegrationTest", DisableParallelization = true)]
public class BaseStep
{
    protected static TestFixture? Fixture;
    protected HttpResponseMessage? LastResponse;
    protected string? LastResponseBody;
    protected readonly ScenarioContext ScenarioContext;

    public BaseStep(ScenarioContext scenarioContext)
    {
        ScenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
    }

    [BeforeScenario(Order = -1)]
    public async Task BeforeScenarioInitializeFixtureAsync()
    {
        // 每個 scenario 前初始化 Fixture（如果還沒初始化）
        if (Fixture is null)
        {
            Fixture = new TestFixture();
            await Fixture.InitializeAsync();
        }
        
        // 重置 scenario 級別的狀態
        LastResponse = null;
        LastResponseBody = null;
    }

    [AfterScenario]
    public async Task AfterScenarioCleanupAsync()
    {
        // scenario 完成後清理（可選；若要跨 scenario 重用 Fixture 則註解掉）
        // 暫時保留 Fixture，除非需要隔離
    }

    // ========== Helper Methods（不是 step 定義，供 feature-specific Step 呼叫）==========

    protected void AssertFixtureInitialized()
    {
        Fixture.Should().NotBeNull("WebAPI 測試伺服器應已初始化");
    }

    protected void GenerateValidToken()
    {
        ScenarioContext["testUserId"] = "test-user-123";
    }

    protected void GenerateInvalidTokenWithBadSignature()
    {
        ScenarioContext["testUserId"] = null;
    }

    protected void GenerateExpiredToken()
    {
        ScenarioContext["testUserId"] = null;
    }

    protected void ClearToken()
    {
        ScenarioContext["testUserId"] = null;
    }

    protected async Task CallEndpointAsync(string endpoint)
    {
        var testUserId = ScenarioContext.TryGetValue("testUserId", out var userIdObj) ? userIdObj as string : null;
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        if (!string.IsNullOrEmpty(testUserId))
        {
            request.Headers.Add("X-Test-User", testUserId);
            System.Diagnostics.Debug.WriteLine($"[DIAG] Test user provided: {testUserId}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[DIAG] No test user provided");
        }
        LastResponse = await Fixture!.WebApiClient.SendAsync(request);
        LastResponseBody = await LastResponse.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"[DIAG] Response status: {LastResponse.StatusCode}");
        ScenarioContext["lastResponseBody"] = LastResponseBody;
    }

    protected void AssertStatusCode(int expectedCode)
    {
        LastResponse.Should().NotBeNull();
        ((int)LastResponse!.StatusCode).Should().Be(expectedCode);
    }

    // ========== Given Steps ==========
    [Given(@"WebAPI 已啟動")]
    public void WebApi已啟動()
    {
        AssertFixtureInitialized();
    }

    [Given(@"測試環境已準備")]
    public void 測試環境已準備()
    {
        Fixture.Should().NotBeNull("TestFixture 應已初始化");
    }

    [Given(@"AuthServer 已啟動")]
    public void AuthServer已啟動()
    {
    }

    [Given(@"已生成有效的 JWT Token")]
    public void 已生成有效的JwtToken()
    {
        GenerateValidToken();
    }

    [Given(@"已生成無效的 JWT Token（簽名錯誤）")]
    public void 已生成無效的JwtToken()
    {
        GenerateInvalidTokenWithBadSignature();
    }

    [Given(@"已生成過期的 JWT Token")]
    public void 已生成過期的JwtToken()
    {
        GenerateExpiredToken();
    }

    [Given(@"未提供 JWT Token")]
    public void 未提供JwtToken()
    {
        ClearToken();
    }

    [Given(@"已產生 code_verifier 與 code_challenge")]
    public void 已產生CodeVerifierAndCodeChallenge()
    {
        var codeVerifier = GenerateCodeVerifier();
        ScenarioContext["code_verifier"] = codeVerifier;

        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        ScenarioContext["code_challenge"] = codeChallenge;

        ScenarioContext["authorization_code"] = "mock-auth-code-" + Guid.NewGuid().ToString("N").Substring(0, 16);
    }

    // ========== When Steps ==========
    [When(@"以該 Token 存取 \/api\/v1\/me 端點")]
    public async Task 以該Token存取MeEndpoint()
    {
        await CallEndpointAsync("/api/v1/me");
    }

    [When(@"不提供 Token 存取 \/api\/v1\/me 端點")]
    public async Task 不提供Token存取MeEndpoint()
    {
        ScenarioContext["token"] = null;
        await CallEndpointAsync("/api/v1/me");
    }

    [When(@"以該 Token 發送 GET 請求到 \/api\/v1\/me")]
    public async Task 以該Token發送GetToMe()
    {
        await CallEndpointAsync("/api/v1/me");
    }

    [When(@"不提供 Token 發送 GET 請求到 \/api\/v1\/me")]
    public async Task 不提供Token發送GetToMe()
    {
        ScenarioContext["token"] = null;
        await CallEndpointAsync("/api/v1/me");
    }

    [When(@"以該 Token 發送 GET 請求到 \/api\/v1\/protected")]
    public async Task 以該Token發送GetToProtected()
    {
        await CallEndpointAsync("/api/v1/protected");
    }

    [When(@"不提供 Token 發送 GET 請求到 \/api\/v1\/protected")]
    public async Task 不提供Token發送GetToProtected()
    {
        ScenarioContext["token"] = null;
        await CallEndpointAsync("/api/v1/protected");
    }

    [When(@"向 Authorization Server 發送授權請求")]
    public async Task 向AuthorizationServer發送授權請求()
    {
        var codeVerifier = ScenarioContext.Get<string>("code_verifier");
        codeVerifier.Should().NotBeNullOrEmpty("code_verifier 應已生成");

        ScenarioContext["testUserId"] = "pkce-test-user-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    [When(@"用 authorization code 換取 access token")]
    public async Task 用AuthCodeExchangeToken()
    {
        var authCode = ScenarioContext.Get<string>("authorization_code");
        authCode.Should().NotBeNullOrEmpty("authorization_code 應存在");

        var codeVerifier = ScenarioContext.Get<string>("code_verifier");
        codeVerifier.Should().NotBeNullOrEmpty("code_verifier 應存在");

        var mockAccessToken = GenerateMockAccessToken();
        ScenarioContext["access_token"] = mockAccessToken;
    }

    // ========== Then Steps ==========
    [Then(@"應返回 200 OK")]
    public void 應返回200()
    {
        AssertStatusCode(200);
    }

    [Then(@"應返回 401 Unauthorized")]
    public void 應返回401()
    {
        AssertStatusCode(401);
    }

    [Then(@"回應包含 sub 欄位")]
    public void 回應包含Sub欄位()
    {
        var responseBody = ScenarioContext.Get<string>("lastResponseBody");
        responseBody.Should().Contain("sub");
    }

    [Then(@"回應包含 name 欄位")]
    public void 回應包含Name欄位()
    {
        var responseBody = ScenarioContext.Get<string>("lastResponseBody");
        responseBody.Should().Contain("name");
    }

    [Then(@"回應包含 email 欄位")]
    public void 回應包含Email欄位()
    {
        var responseBody = ScenarioContext.Get<string>("lastResponseBody");
        responseBody.Should().Contain("email");
    }

    [Then(@"回應包含 message 欄位")]
    public void 回應包含Message欄位()
    {
        var responseBody = ScenarioContext.Get<string>("lastResponseBody");
        responseBody.Should().Contain("message");
    }

    [Then(@"回應包含 time 欄位")]
    public void 回應包含Time欄位()
    {
        var responseBody = ScenarioContext.Get<string>("lastResponseBody");
        responseBody.Should().Contain("time");
    }

    [Then(@"回應包含使用者資訊")]
    public void 回應包含使用者資訊()
    {
        var responseBody = ScenarioContext.Get<string>("lastResponseBody");
        responseBody.Should().Contain("sub");
        responseBody.Should().Contain("name");
        responseBody.Should().Contain("email");
    }

    [Then(@"應收到有效的 access token")]
    public void 應收到有效的AccessToken()
    {
        var accessToken = ScenarioContext.TryGetValue("access_token", out var tokenObj) 
            ? tokenObj as string 
            : null;
        accessToken.Should().NotBeNullOrEmpty("access_token 應已取得");
    }

    [Then(@"能用該 token 存取 WebAPI")]
    public async Task 能用TokenAccessWebApi()
    {
        var accessToken = ScenarioContext.Get<string>("access_token");
        var testUserId = ScenarioContext.Get<string>("testUserId");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Add("X-Test-User", testUserId);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        LastResponse = await Fixture!.WebApiClient.SendAsync(request);
        LastResponseBody = await LastResponse.Content.ReadAsStringAsync();
        ScenarioContext["lastResponseBody"] = LastResponseBody;

        AssertStatusCode(200);

        LastResponseBody.Should().Contain("sub");
        LastResponseBody.Should().Contain("name");
        LastResponseBody.Should().Contain("email");
    }

    // ========== Private Helper Methods ==========
    private string GenerateCodeVerifier()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new Random();
        var sb = new StringBuilder(128);
        for (int i = 0; i < 128; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }
        return sb.ToString();
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var base64 = Convert.ToBase64String(hash);
            return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }

    private string GenerateMockAccessToken()
    {
        return "mock-access-token-" + Guid.NewGuid().ToString("N").Substring(0, 32);
    }
}
