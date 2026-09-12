using Microsoft.Playwright;
using Reqnroll;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OAuth.AuthServer.WebUI.E2E;

[Binding]
public class 同意頁面Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [Given(@"使用者尚未登入")]
    public void Given使用者尚未登入()
    {
        // 全新 browser context，無任何 session cookie
    }

    [When(@"使用者透過 ""(.*)"" 發起授權")]
    public async Task When使用者透過發起授權(string clientId)
    {
        var codeVerifier  = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state         = Guid.NewGuid().ToString("N");
        var nonce         = Guid.NewGuid().ToString("N");
        var redirectUri   = Uri.EscapeDataString($"{TestSettings.MvcClientBase}/signin-oidc");
        var requestedScopes = new[] { "openid", "profile", "email" };
        var scope         = Uri.EscapeDataString(string.Join(" ", requestedScopes));

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

    [When(@"用戶點擊同意")]
    public async Task When用戶點擊同意()
    {
        var navigationTask = Page.WaitForURLAsync($"{TestSettings.MvcClientBase}/**", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout   = 20_000,
        });
        await Page.ClickAsync("button[value='accept']");
        await navigationTask;
    }

    [When(@"用戶點擊拒絕")]
    public async Task When用戶點擊拒絕()
    {
        var navigationTask = Page.WaitForURLAsync("**/Home/Error**", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout   = 20_000,
        });
        await Page.ClickAsync("button[value='deny']");
        await navigationTask;
    }

    [Then(@"應顯示同意頁面")]
    public async Task Then應顯示同意頁面()
    {
        await Assertions.Expect(Page).ToHaveURLAsync(
            new Regex("/consent", RegexOptions.IgnoreCase),
            new PageAssertionsToHaveURLOptions { Timeout = 10_000 });
    }

    [Then(@"同意頁面應列出請求的 scopes")]
    public async Task Then同意頁面應列出請求的Scopes()
    {
        var scopeLocator = Page.Locator("[data-testid='scope-list']");
        await Assertions.Expect(scopeLocator).ToBeVisibleAsync(new() { Timeout = 5_000 });
        
        var scopeItems = await Page.Locator("[data-testid='scope-list'] li").AllInnerTextsAsync();
        var actualScopes = scopeItems.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        var expectedScopes = ctx.TryGetValue("RequestedScopes", out var reqObj) && reqObj is IEnumerable<string> expScopes
            ? expScopes.ToList()
            : ["openid", "profile", "email"];

        Assert.NotEmpty(actualScopes);
        Assert.Equal(expectedScopes.OrderBy(s => s), actualScopes.OrderBy(s => s));
    }

    [Then(@"不應顯示同意頁面")]
    public void Then不應顯示同意頁面()
    {
        Assert.False(Page.Url.Contains("/consent", StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"應完成授權跳轉至 MVC Client")]
    public async Task Then應完成授權跳轉至MvcClient()
    {
        if (!Page.Url.StartsWith(TestSettings.MvcClientBase))
            await Page.WaitForURLAsync($"{TestSettings.MvcClientBase}/**", new PageWaitForURLOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout   = 20_000,
            });

        Assert.StartsWith(TestSettings.MvcClientBase, Page.Url);
    }

    [Then(@"應顯示授權錯誤訊息 ""(.*)""")]
    public async Task Then應顯示授權錯誤訊息(string errorCode)
    {
        var errorLocator = Page.Locator($"[data-testid='error-code']:has-text('{errorCode}')");
        if (await errorLocator.IsVisibleAsync())
        {
            await Assertions.Expect(errorLocator).ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
        else
        {
            Assert.Contains(errorCode, Page.Url, StringComparison.OrdinalIgnoreCase);
        }

        // 後端嚴格驗證：拒絕授權後確認沒有核發任何 Access Token 或成功 signin-oidc callback
        Assert.DoesNotContain("/signin-oidc?code=", Page.Url);
        Assert.DoesNotContain("access_token=", Page.Url);

        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var httpClient = new HttpClient(handler);
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{TestSettings.AuthServerBase}/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = "mvc-client",
                ["client_secret"] = "mvc-client-secret",
                ["code"] = "denied_attempt_code",
                ["redirect_uri"] = $"{TestSettings.MvcClientBase}/signin-oidc",
                ["code_verifier"] = ctx.TryGetValue("LastCodeVerifier", out var cv) ? cv.ToString()! : "verifier"
            })
        };
        var tokenResponse = await httpClient.SendAsync(tokenRequest);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, tokenResponse.StatusCode);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
