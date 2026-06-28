using Microsoft.Playwright;
using Reqnroll;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OAuth.Clients.PlaywrightTest;

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
        var scope         = Uri.EscapeDataString("openid profile email");

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
            new Regex("/Connect/Consent"),
            new PageAssertionsToHaveURLOptions { Timeout = 10_000 });
    }

    [Then(@"同意頁面應列出請求的 scopes")]
    public async Task Then同意頁面應列出請求的Scopes()
    {
        await Assertions.Expect(Page.Locator("[data-testid='scope-list']"))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Then(@"不應顯示同意頁面")]
    public void Then不應顯示同意頁面()
    {
        Assert.DoesNotContain("/Connect/Consent", Page.Url);
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
        await Assertions.Expect(
            Page.Locator($"[data-testid='error-code']:has-text('{errorCode}')")
        ).ToBeVisibleAsync(new() { Timeout = 5_000 });
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
