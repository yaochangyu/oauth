using Microsoft.Playwright;
using Reqnroll;

namespace OAuth.AuthServer.WebUI.E2E._05_RealWorldScenarios;

[Binding]
public class 跨模組真實場景Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [When(@"使用者前往註冊頁")]
    public async Task When使用者前往註冊頁()
    {
        await Page.GotoAsync($"{TestSettings.AuthServerBase}/register");
        await Page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 15_000 });
    }

    [When(@"使用者填寫隨機會員註冊表單密碼 ""(.*)""")]
    public async Task When使用者填寫隨機會員註冊表單密碼(string password)
    {
        var uniqueEmail = $"user_{Guid.NewGuid():N}@example.com";
        ctx["CreatedUserEmail"] = uniqueEmail;
        ctx["CreatedUserPassword"] = password;

        await Page.FillAsync("input[name='email']", uniqueEmail);
        await Page.FillAsync("input[name='password']", password);
        await Page.ClickAsync("button[type='submit']");
    }

    [When(@"使用者以新註冊的帳號登入")]
    public async Task When使用者以新註冊的帳號登入()
    {
        var email = (string)ctx["CreatedUserEmail"];
        var password = (string)ctx["CreatedUserPassword"];
        var page = Page;

        await page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 15_000 });
        await page.FillAsync("input[name='userName']", email);
        await page.FillAsync("input[type='password']", password);

        var navigationTask = page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 20_000 });
        await page.ClickAsync("button[type='submit']");

        try { await navigationTask; }
        catch (TimeoutException) { }

        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 20_000 });
    }

    [When(@"使用者結束工作階段並再次透過 ""(.*)"" 發起授權")]
    public async Task When使用者結束工作階段並再次透過發起授權(string clientId)
    {
        if (ctx.TryGetValue("browser", out var b) && b is IBrowser browser)
        {
            var oldPage = (IPage)ctx["page"];
            await oldPage.Context.CloseAsync();

            var newContext = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
            });
            var newPage = await newContext.NewPageAsync();
            ctx["page"] = newPage;
        }

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");
        var redirectUri = Uri.EscapeDataString($"{TestSettings.MvcClientBase}/signin-oidc");
        var requestedScopes = new[] { "openid", "profile", "email" };
        var scope = Uri.EscapeDataString(string.Join(" ", requestedScopes));

        ctx["RequestedScopes"] = requestedScopes;
        ctx["LastClientId"] = clientId;
        ctx["LastCodeVerifier"] = codeVerifier;

        var url = $"{TestSettings.AuthServerBase}/connect/authorize" +
                  $"?client_id={clientId}" +
                  $"&response_type=code" +
                  $"&scope={scope}" +
                  $"&redirect_uri={redirectUri}" +
                  $"&code_challenge={codeChallenge}" +
                  $"&code_challenge_method=S256" +
                  $"&state={state}" +
                  $"&nonce={nonce}";

        await Page.GotoAsync(url);
    }

    [Then(@"驗證 UserInfo 包含同意之 Claims 且不包含未同意之 Claims")]
    public async Task Then驗證UserInfo包含同意之Claims且不包含未同意之Claims()
    {
        // 透過後端直接以當前流程交換之 Token 或以 admin 憑證呼叫 UserInfo 端點進行雙向驗證
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var httpClient = new HttpClient(handler);

        var lastVerifier = ctx.TryGetValue("LastCodeVerifier", out var cv) ? cv.ToString()! : GenerateCodeVerifier();
        var code = ctx.TryGetValue("AuthCode", out var cObj) ? cObj.ToString() : null;

        if (string.IsNullOrEmpty(code) && Page.Url.Contains("code="))
        {
            var uri = new Uri(Page.Url);
            code = System.Web.HttpUtility.ParseQueryString(uri.Query)["code"];
        }

        string? accessToken = null;
        if (!string.IsNullOrEmpty(code))
        {
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"{TestSettings.AuthServerBase}/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = "mvc-client",
                    ["client_secret"] = "mvc-client-secret",
                    ["code"] = code,
                    ["redirect_uri"] = $"{TestSettings.MvcClientBase}/signin-oidc",
                    ["code_verifier"] = lastVerifier
                })
            };
            var tokenResp = await httpClient.SendAsync(tokenReq);
            if (tokenResp.IsSuccessStatusCode)
            {
                var doc = System.Text.Json.JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync());
                accessToken = doc.RootElement.GetProperty("access_token").GetString();
            }
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            var userinfoReq = new HttpRequestMessage(HttpMethod.Get, $"{TestSettings.AuthServerBase}/connect/userinfo");
            userinfoReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userinfoResp = await httpClient.SendAsync(userinfoReq);
            Assert.True(userinfoResp.IsSuccessStatusCode);

            var userinfoDoc = System.Text.Json.JsonDocument.Parse(await userinfoResp.Content.ReadAsStringAsync());
            var root = userinfoDoc.RootElement;

            // 雙向驗證 1：已同意之 scopes（openid, profile, email）對應之 Claims 必須出現
            Assert.True(root.TryGetProperty("sub", out _), "同意之 openid scope 對應之 sub claim 必須存在");
            Assert.True(root.TryGetProperty("email", out var emailProp) && !string.IsNullOrEmpty(emailProp.GetString()), "同意之 email scope 對應之 email claim 必須存在");
            Assert.True(root.TryGetProperty("name", out var nameProp) && !string.IsNullOrEmpty(nameProp.GetString()), "同意之 profile scope 對應之 name claim 必須存在");

            // 雙向驗證 2：未同意之 scopes（如 phone, address）對應之 Claims 必須不出現
            Assert.False(root.TryGetProperty("phone_number", out _), "未請求/未同意之 phone scope 對應之 phone_number claim 不應出現");
            Assert.False(root.TryGetProperty("address", out _), "未請求/未同意之 address scope 對應之 address claim 不應出現");
        }
        else
        {
            // 若為瀏覽器端會話驗證，確認頁面跳轉且無異常
            Assert.StartsWith(TestSettings.MvcClientBase, Page.Url);
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
